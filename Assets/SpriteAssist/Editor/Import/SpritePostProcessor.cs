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
                UpdateMesh(sprites);
            }

            if (SpriteAssistSettings.instance.enableRenameMeshPrefabAutomatically)
            {
                RenameMeshPrefab(assetPath);
            }
        }

        private void UpdateMesh(Sprite[] sprites)
        {
            foreach (var sprite in sprites)
            {
                SpriteImportData importData = new SpriteImportData(sprite, assetPath);
                SpriteConfigData configData = SpriteConfigData.GetData(importData.textureImporter.userData);
                MeshCreatorBase meshCreator = MeshCreatorBase.GetInstance(configData.mode);

                MeshPrefabService.OverrideGeometry(importData, meshCreator, configData);
                MeshPrefabService.UpdateMeshInMeshPrefab(importData, meshCreator, configData);
                break;
            }
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
