using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public class SpritePostProcessor : AssetPostprocessor
    {
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
            if (PrefabUtil.IsAssetImportWorkerProcess()) return;

            foreach (string importedAssetPath in importedAssets)
            {
                if (!SpriteAssistSettings.instance.ShouldProcessSprite(importedAssetPath)) continue;

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(importedAssetPath);

                if (sprite == null) continue;

                SpriteInspector.isSpriteReloaded = true;

                SpriteImportData importData = new SpriteImportData(sprite, importedAssetPath);

                UpdateMeshPrefab(importData);

                if (SpriteAssistSettings.instance.enableRenameMeshPrefabAutomatically)
                {
                    RenameMeshPrefab(importData);
                }
            }
        }

        private void OverrideSpriteGeometry(Sprite[] sprites)
        {
            if (TryResolveFirstSprite(sprites, out SpriteImportData importData, out MeshCreatorBase meshCreator, out SpriteConfigData configData))
            {
                MeshPrefabService.OverrideGeometry(importData, meshCreator, configData);
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

        private static void UpdateMeshPrefab(SpriteImportData importData)
        {
            // Mesh Prefab を持つ single sprite のみ更新対象
            if (!importData.HasMeshPrefab) return;
            if (!importData.textureImporterSettings.IsSingleSprite()) return;

            SpriteConfigData configData = SpriteConfigData.GetData(importData.textureImporter.userData);
            MeshCreatorBase meshCreator = MeshCreatorBase.GetInstance(configData.mode);
            MeshPrefabService.UpdateMeshInMeshPrefab(importData, meshCreator, configData);
        }

        private static void RenameMeshPrefab(SpriteImportData importData)
        {
            string assetPath = importData.assetPath;
            GameObject meshPrefab = importData.MeshPrefab;

            if (meshPrefab == null) return;

            // import サイクル完了後に rename する（import 中の asset 移動を避ける）
            EditorApplication.delayCall += () =>
            {
                PrefabUtil.TryRename(assetPath, meshPrefab);
            };
        }
    }
}
