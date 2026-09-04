using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    // Mesh Prefab の Mesh はテクスチャ import の成果物（テクスチャのサブアセット）として出力する。
    // これにより Mesh 更新は Unity の import 依存関係に乗り、強制終了後の再開・Accelerator キャッシュ・Parallel Import で
    // 欠落や不整合が起きない。prefab ファイル自体は import 中には一切書き換えない。
    public class SpritePostProcessor : AssetPostprocessor
    {
        // import 出力（サブアセット Mesh）の仕様を変えたら必ず上げる。過去 artifact を無効化するため。
        // v2: outline 元テクスチャの生成を GPU（Blit/ReadPixels）から CPU に変更。-nographics 環境で矩形 Mesh になっていた artifact を無効化する。
        private const uint VERSION = 2;

        private const int MaxRenameAttempts = 3;

        // key: Mesh Prefab GUID, value: sprite asset path
        private static readonly Dictionary<string, string> _pendingRenames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> _renameAttempts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static bool _renameScheduled;

        public override uint GetVersion()
        {
            return VERSION;
        }

        private void OnPostprocessSprites(Texture2D tex, Sprite[] sprites)
        {
            if (!SpriteAssistSettings.instance.ShouldProcessSprite(assetPath)) return;

            TextureImporter textureImporter = assetImporter as TextureImporter;
            TextureImporterSettings textureImporterSettings = new TextureImporterSettings();
            textureImporter!.ReadTextureSettings(textureImporterSettings);

            if (!textureImporterSettings.IsSingleSprite()) return;

            SpriteImportData importData = null;
            try
            {
                if (!TryResolveFirstSprite(sprites, out importData, out MeshCreatorBase meshCreator, out SpriteConfigData configData)) return;

                // import 対象スプライト自体のジオメトリ上書き
                MeshPrefabService.OverrideGeometry(importData, meshCreator, configData);

                // Mesh Prefab がリンクされている場合のみ、prefab 用 Mesh をテクスチャのサブアセットとして出力する
                if (SpriteImportData.HasMeshPrefabLink(textureImporter, assetPath))
                {
                    MeshPrefabService.AddImportMeshes(context, importData, meshCreator, configData);
                }
            }
            catch (Exception e)
            {
                // 黙って Unity 既定ジオメトリ（矩形）で成功させると、壊れた成果物が Accelerator 経由で全マシンに伝播する。
                // import エラーとして明示的に失敗させ、原因をログに残す。
                context.LogImportError($"{e.Message}\n{e.StackTrace}");
            }
            finally
            {
                importData?.Dispose();
            }
        }

        // import 完了後にメインプロセスで呼ばれる。ここでは prefab の内容は触らない。
        // 消えた prefab の remap 掃除と、任意設定の prefab 自動 rename のみ行う。
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (PrefabUtil.IsAssetImportWorkerProcess()) return;

            bool renameAutomatically = SpriteAssistSettings.instance.enableRenameMeshPrefabAutomatically;

            foreach (string importedAssetPath in importedAssets)
            {
                if (!SpriteAssistSettings.instance.ShouldProcessSprite(importedAssetPath)) continue;

                TextureImporter textureImporter = AssetImporter.GetAtPath(importedAssetPath) as TextureImporter;
                if (textureImporter == null) continue;

                if (textureImporter.spriteImportMode != SpriteImportMode.Single) continue;

                SpriteInspector.isSpriteReloaded = true;

                if (SpriteImportData.RemoveMissingExternalPrefab(textureImporter, importedAssetPath))
                {
                    AssetDatabase.WriteImportSettingsIfDirty(importedAssetPath);
                }

                if (!renameAutomatically) continue;
                if (!SpriteImportData.TryGetMeshPrefabPath(textureImporter, importedAssetPath, out string meshPrefabPath)) continue;
                if (Path.GetFileNameWithoutExtension(meshPrefabPath) == Path.GetFileNameWithoutExtension(importedAssetPath)) continue;

                // path は flush までに変わり得るので GUID で保持する
                string meshPrefabGuid = AssetDatabase.AssetPathToGUID(meshPrefabPath);
                if (string.IsNullOrEmpty(meshPrefabGuid)) continue;

                _pendingRenames[meshPrefabGuid] = importedAssetPath;
            }

            ScheduleRename();
        }

        private bool TryResolveFirstSprite(Sprite[] sprites, out SpriteImportData importData, out MeshCreatorBase meshCreator, out SpriteConfigData configData)
        {
            // 先頭スプライトのみ対象（既存仕様を踏襲）
            foreach (var sprite in sprites)
            {
                importData = new SpriteImportData(sprite, assetPath);
                configData = SpriteConfigData.GetData(importData.textureImporter.userData);
                meshCreator = MeshCreatorBase.GetInstance(configData.mode);
                return true;
            }

            importData = null;
            meshCreator = null;
            configData = null;
            return false;
        }

        private static void ScheduleRename()
        {
            if (_renameScheduled || _pendingRenames.Count == 0) return;

            _renameScheduled = true;
            EditorApplication.delayCall += FlushRenames;
        }

        private static void FlushRenames()
        {
            _renameScheduled = false;
            if (_pendingRenames.Count == 0) return;

            var renames = new List<KeyValuePair<string, string>>(_pendingRenames);
            _pendingRenames.Clear();

            // StartAssetEditing で batch 化すると、直後の move と AssetDatabase の状態がずれることがあるため個別に rename する。
            // 直前に move された asset は同一フレームでは rename できないことがあるため、失敗分は次フレームで再試行する。
            foreach (KeyValuePair<string, string> rename in renames)
            {
                if (TryRename(rename.Key, rename.Value, out string error))
                {
                    _renameAttempts.Remove(rename.Key);
                    continue;
                }

                _renameAttempts.TryGetValue(rename.Key, out int attempts);
                if (attempts + 1 >= MaxRenameAttempts)
                {
                    _renameAttempts.Remove(rename.Key);
                    Debug.LogWarning($"[SpriteAssist] Failed to rename Mesh Prefab '{AssetDatabase.GUIDToAssetPath(rename.Key)}': {error}");
                    continue;
                }

                _renameAttempts[rename.Key] = attempts + 1;
                _pendingRenames[rename.Key] = rename.Value;
            }

            ScheduleRename();
        }

        private static bool TryRename(string meshPrefabGuid, string spriteAssetPath, out string error)
        {
            error = null;
            string meshPrefabPath = AssetDatabase.GUIDToAssetPath(meshPrefabGuid);
            if (string.IsNullOrEmpty(meshPrefabPath))
            {
                error = "Mesh Prefab not found";
                return false;
            }

            string spriteName = Path.GetFileNameWithoutExtension(spriteAssetPath);
            if (Path.GetFileNameWithoutExtension(meshPrefabPath) == spriteName) return true;

            error = AssetDatabase.RenameAsset(meshPrefabPath, spriteName);
            return string.IsNullOrEmpty(error);
        }
    }
}
