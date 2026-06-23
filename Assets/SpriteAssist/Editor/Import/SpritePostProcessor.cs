using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public class SpritePostProcessor : AssetPostprocessor
    {
        private void OnPostprocessSprites(Texture2D tex, Sprite[] sprites)
        {
            if (!SpriteAssistSettings.instance.ShouldProcessSprite(assetPath)) return;

            SpriteInspector.isSpriteReloaded = true;

            TextureImporter textureImporter = assetImporter as TextureImporter;
            TextureImporterSettings textureImporterSettings = new TextureImporterSettings();
            textureImporter!.ReadTextureSettings(textureImporterSettings);

            if (textureImporterSettings.IsSingleSprite())
            {
                // import 対象スプライト自体のジオメトリ上書き（import worker 上で完結する処理）
                OverrideSpriteGeometry(sprites);
                // 外部 Mesh Prefab の更新（将来 OnPostprocessAllAssets へ移す対象）
                UpdateMeshPrefab(sprites);
            }

            if (SpriteAssistSettings.instance.enableRenameMeshPrefabAutomatically)
            {
                RenameMeshPrefab(assetPath);
            }
        }

        private void OverrideSpriteGeometry(Sprite[] sprites)
        {
            if (TryResolveFirstSprite(sprites, out SpriteImportData importData, out MeshCreatorBase meshCreator, out SpriteConfigData configData))
            {
                MeshPrefabService.OverrideGeometry(importData, meshCreator, configData);
            }
        }

        private void UpdateMeshPrefab(Sprite[] sprites)
        {
            if (TryResolveFirstSprite(sprites, out SpriteImportData importData, out MeshCreatorBase meshCreator, out SpriteConfigData configData))
            {
                MeshPrefabService.UpdateMeshInMeshPrefab(importData, meshCreator, configData);
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

        private static void RenameMeshPrefab(string assetPath)
        {
            EditorApplication.delayCall += () =>
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                SpriteImportData importData = new SpriteImportData(sprite, assetPath);

                if (importData.HasMeshPrefab)
                {
                    PrefabUtil.TryRename(importData.assetPath, importData.MeshPrefab);
                }
            };
        }
    }
}
