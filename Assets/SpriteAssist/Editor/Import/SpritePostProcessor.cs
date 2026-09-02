using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpriteAssist
{
    public class SpritePostProcessor : AssetPostprocessor
    {
        private const int BulkUnloadThreshold = 100;
        private static readonly Dictionary<string, string> _pendingMeshPrefabs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static bool _flushScheduled;
        private static bool _isFlushingMeshPrefabs;
        private static bool _isBulkFlush;

        private void OnPostprocessSprites(Texture2D tex, Sprite[] sprites)
        {
            if (!SpriteAssistSettings.instance.ShouldProcessSprite(assetPath)) return;

            TextureImporter textureImporter = assetImporter as TextureImporter;
            TextureImporterSettings textureImporterSettings = new TextureImporterSettings();
            textureImporter!.ReadTextureSettings(textureImporterSettings);

            if (textureImporterSettings.IsSingleSprite())
            {
                // import 対象スプライト自体のジオメトリ上書き。import worker 上で完結する処理なのでここで実行する
                OverrideSpriteGeometry(sprites);
            }
        }

        // import 完了後にメインプロセスで呼ばれる。Parallel Import 時、外部アセット(Mesh Prefab)の変更は
        // worker プロセスではメインの AssetDatabase に反映されないため、外部 prefab の更新・rename はここで行う
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            // worker プロセスでは外部アセットを変更しない（メインプロセスのみで処理）
            if (PrefabUtil.IsAssetImportWorkerProcess() || _isFlushingMeshPrefabs) return;

            foreach (string importedAssetPath in importedAssets)
            {
                if (!SpriteAssistSettings.instance.ShouldProcessSprite(importedAssetPath)) continue;

                TextureImporter textureImporter = AssetImporter.GetAtPath(importedAssetPath) as TextureImporter;
                if (textureImporter == null) continue;

                if (textureImporter.spriteImportMode != SpriteImportMode.Single) continue;

                if (SpriteImportData.RemoveMissingExternalPrefab(textureImporter, importedAssetPath))
                {
                    AssetDatabase.WriteImportSettingsIfDirty(importedAssetPath);
                }

                if (!TryGetMeshPrefabPath(textureImporter, importedAssetPath, out string meshPrefabPath)) continue;

                QueueMeshPrefab(importedAssetPath, meshPrefabPath);
            }

            ScheduleMeshPrefabFlush();
        }

        private void OverrideSpriteGeometry(Sprite[] sprites)
        {
            if (TryResolveFirstSprite(sprites, out SpriteImportData importData, out MeshCreatorBase meshCreator, out SpriteConfigData configData))
            {
                using (importData)
                {
                    MeshPrefabService.OverrideGeometry(importData, meshCreator, configData);
                }
            }
        }

        // 先頭スプライトのみ対象（既存仕様を踏襲）
        private bool TryResolveFirstSprite(Sprite[] sprites, out SpriteImportData importData, out MeshCreatorBase meshCreator, out SpriteConfigData configData)
        {
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

        private static bool TryGetMeshPrefabPath(TextureImporter textureImporter, string spriteAssetPath, out string meshPrefabPath)
        {
            string legacyIdentifier = Path.GetFileNameWithoutExtension(spriteAssetPath);
            foreach (KeyValuePair<AssetImporter.SourceAssetIdentifier, Object> externalObject in textureImporter.GetExternalObjectMap())
            {
                if ((externalObject.Key.name != SpriteImportData.MESH_PREFAB_IDENTIFIER && externalObject.Key.name != legacyIdentifier) ||
                    externalObject.Value is not GameObject prefab)
                {
                    continue;
                }

                meshPrefabPath = AssetDatabase.GetAssetPath(prefab);
                if (!string.IsNullOrEmpty(meshPrefabPath))
                {
                    return true;
                }
            }

            meshPrefabPath = null;
            return false;
        }

        private static void QueueMeshPrefab(string spriteAssetPath, string meshPrefabPath)
        {
            _pendingMeshPrefabs[meshPrefabPath] = spriteAssetPath;
        }

        private static void ScheduleMeshPrefabFlush()
        {
            if (_flushScheduled || _pendingMeshPrefabs.Count == 0) return;

            _flushScheduled = true;
            EditorApplication.delayCall += FlushMeshPrefabs;
        }

        private static void FlushMeshPrefabs()
        {
            _flushScheduled = false;
            if (_pendingMeshPrefabs.Count == 0) return;

            _isBulkFlush |= _pendingMeshPrefabs.Count >= BulkUnloadThreshold;
            _isFlushingMeshPrefabs = true;
            var processedMeshPrefabs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var dirtyMeshPrefabs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // 100 件ごとに unload しながら全 queue を処理し、rename と local import は最後に一度だけ実行する。
                // batch ごとに StopAssetEditing の refresh を挟むと大規模 import で数時間かかるため。
                while (_pendingMeshPrefabs.Count > 0)
                {
                    var pendingMeshPrefabs = new Dictionary<string, string>(BulkUnloadThreshold, StringComparer.OrdinalIgnoreCase);
                    foreach (KeyValuePair<string, string> pendingMeshPrefab in _pendingMeshPrefabs)
                    {
                        pendingMeshPrefabs.Add(pendingMeshPrefab.Key, pendingMeshPrefab.Value);
                        if (pendingMeshPrefabs.Count == BulkUnloadThreshold) break;
                    }

                    foreach (KeyValuePair<string, string> pendingMeshPrefab in pendingMeshPrefabs)
                    {
                        _pendingMeshPrefabs.Remove(pendingMeshPrefab.Key);
                        processedMeshPrefabs.Add(pendingMeshPrefab.Key, pendingMeshPrefab.Value);
                        if (UpdateMeshPrefab(pendingMeshPrefab.Value, pendingMeshPrefab.Key))
                        {
                            dirtyMeshPrefabs.Add(pendingMeshPrefab.Key, pendingMeshPrefab.Value);
                            SaveMeshPrefabIfDirty(pendingMeshPrefab.Key);
                        }
                    }

                    if (_isBulkFlush)
                    {
                        EditorUtility.UnloadUnusedAssetsImmediate();
                    }
                }

                ImportUpdatedMeshPrefabs(processedMeshPrefabs, dirtyMeshPrefabs);
            }
            catch
            {
                ImportUpdatedMeshPrefabs(processedMeshPrefabs, dirtyMeshPrefabs);
                throw;
            }
            finally
            {
                _isFlushingMeshPrefabs = false;
                _isBulkFlush = false;
                ScheduleMeshPrefabFlush();
            }
        }

        private static void ImportUpdatedMeshPrefabs(Dictionary<string, string> processedMeshPrefabs,
            Dictionary<string, string> dirtyMeshPrefabs)
        {
            var importPaths = new HashSet<string>(dirtyMeshPrefabs.Keys, StringComparer.OrdinalIgnoreCase);
            bool shouldRename = SpriteAssistSettings.instance.enableRenameMeshPrefabAutomatically &&
                                NeedsMeshPrefabRename(processedMeshPrefabs);
            if (importPaths.Count == 0 && !shouldRename) return;

            AssetDatabase.StartAssetEditing();
            try
            {
                if (shouldRename)
                {
                    RenameMeshPrefabs(processedMeshPrefabs, importPaths);
                }

                ImportMeshPrefabs(importPaths);
            }
            finally
            {
                // rename と import の間で自動 refresh させず、必ず local artifact を生成する。
                AssetDatabase.StopAssetEditing();
            }
        }

        private static bool UpdateMeshPrefab(string spriteAssetPath, string meshPrefabPath)
        {
            TextureImporter textureImporter = AssetImporter.GetAtPath(spriteAssetPath) as TextureImporter;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spriteAssetPath);
            GameObject meshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(meshPrefabPath);
            if (textureImporter == null || sprite == null || meshPrefab == null) return false;

            SpriteInspector.isSpriteReloaded = true;
            SpriteConfigData configData = SpriteConfigData.GetData(textureImporter.userData);
            if (configData.mode == SpriteConfigData.Mode.ComplexMesh)
            {
                using SpriteImportData importData = new SpriteImportData(sprite, textureImporter, spriteAssetPath);
                return MeshPrefabService.UpdateMeshInMeshPrefab(importData, MeshCreatorBase.GetInstance(configData.mode), configData);
            }

            return MeshPrefabService.UpdateMeshInMeshPrefabFromImportedSprite(meshPrefab, sprite,
                new TextureInfo(sprite, spriteAssetPath), configData);
        }

        private static void SaveMeshPrefabIfDirty(string meshPrefabPath)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(meshPrefabPath))
            {
                if (EditorUtility.IsDirty(asset))
                {
                    AssetDatabase.SaveAssetIfDirty(asset);
                }
            }
        }

        private static void ImportMeshPrefabs(HashSet<string> importPaths)
        {
            if (importPaths.Count == 0) return;

            foreach (string meshPrefabPath in importPaths)
            {
                // 変更した prefab は Accelerator から取得せず、ローカルで artifact を更新する。
                AssetDatabase.ImportAsset(meshPrefabPath,
                    ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);
            }
        }

        private static void RenameMeshPrefabs(Dictionary<string, string> pendingMeshPrefabs, HashSet<string> importPaths)
        {
            foreach (KeyValuePair<string, string> pendingMeshPrefab in pendingMeshPrefabs)
            {
                string meshPrefabPath = pendingMeshPrefab.Key;
                string spriteName = Path.GetFileNameWithoutExtension(pendingMeshPrefab.Value);
                if (Path.GetFileNameWithoutExtension(meshPrefabPath) == spriteName) continue;

                string error = AssetDatabase.RenameAsset(meshPrefabPath, spriteName);
                if (!string.IsNullOrEmpty(error)) continue;

                importPaths.Remove(meshPrefabPath);
                string renamedPath = Path.Combine(Path.GetDirectoryName(meshPrefabPath)!, spriteName + ".prefab");
                importPaths.Add(renamedPath.Replace('\\', '/'));
            }
        }

        private static bool NeedsMeshPrefabRename(Dictionary<string, string> pendingMeshPrefabs)
        {
            foreach (KeyValuePair<string, string> pendingMeshPrefab in pendingMeshPrefabs)
            {
                if (Path.GetFileNameWithoutExtension(pendingMeshPrefab.Key) !=
                    Path.GetFileNameWithoutExtension(pendingMeshPrefab.Value))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
