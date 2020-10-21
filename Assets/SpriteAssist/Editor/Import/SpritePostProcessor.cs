using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public class SpritePostProcessor : AssetPostprocessor
    {
        private void OnPostprocessSprites(Texture2D texture, Sprite[] sprites)
        {
            TextureImporter textureImporter = assetImporter as TextureImporter;
            TextureImporterSettings textureImporterSettings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(textureImporterSettings);
            SpriteConfigData configData = SpriteConfigData.GetData(textureImporter.userData);

            if (textureImporterSettings.spriteMeshType != SpriteMeshType.Tight || !configData.overriden)
            {
                return;
            }

            MeshCreator creator = MeshCreator.GetInstnace(configData);

            foreach (var sprite in sprites)
            {
                creator.OverrideGeometry(sprite, configData);
            }
        }
    }
}
