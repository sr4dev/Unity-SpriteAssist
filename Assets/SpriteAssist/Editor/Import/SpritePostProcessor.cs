using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public class SpritePostProcessor : AssetPostprocessor
    {
        private void OnPostprocessTexture(Texture2D _)
        {
            TextureImporter textureImporter = assetImporter as TextureImporter;
            TextureImporterSettings textureImporterSettings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(textureImporterSettings);

            EditorApplication.delayCall += () =>
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

                if (sprite == null)
                {
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                    sprite = SpriteUtil.CreateSprite(texture, textureImporter.spritePivot, textureImporter.spritePixelsPerUnit);
                }

                SpriteProcessor spriteProcessor = new SpriteProcessor(sprite, assetPath);
                spriteProcessor.UpdateSubAssetsInMeshPrefab(spriteProcessor.mainImportData);
                AssetDatabase.SaveAssets();
            };

        }

        private void OnPostprocessSprites(Texture2D _, Sprite[] sprites)
        {
            TextureImporter textureImporter = assetImporter as TextureImporter;
            TextureImporterSettings textureImporterSettings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(textureImporterSettings);

            foreach (var sprite in sprites)
            {
                SpriteProcessor spriteProcessor = new SpriteProcessor(sprite, assetPath);
                spriteProcessor.OverrideGeometry();
            }
        }
    }
}
