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

            //update mesh prefab
            EditorApplication.delayCall += () =>
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                SpriteProcessor spriteProcessor = new SpriteProcessor(sprite, assetPath);
                spriteProcessor.UpdateSubAssetsInMeshPrefab(spriteProcessor.mainImportData);
                AssetDatabase.SaveAssets();
            };
        }
    }
}
