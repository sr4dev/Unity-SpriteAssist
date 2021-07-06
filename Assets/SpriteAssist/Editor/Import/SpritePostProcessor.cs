using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public class SpritePostProcessor : AssetPostprocessor
    {
        private void OnPostprocessSprites(Texture2D _, Sprite[] sprites)
        {
            TextureImporter textureImporter = assetImporter as TextureImporter;
            TextureImporterSettings textureImporterSettings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(textureImporterSettings);

            //override mesh
            foreach (var sprite in sprites)
            {
                SpriteProcessor spriteProcessor = new SpriteProcessor(sprite, assetPath);
                spriteProcessor.OverrideGeometry();
            }

            //auto rename
            if (SpriteAssistSettings.Settings.enableRenameMeshPrefabAutomatically)
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
}
