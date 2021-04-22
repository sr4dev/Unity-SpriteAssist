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

            if (textureImporterSettings.spriteMeshType != SpriteMeshType.Tight || !configData.IsOverriden)
            {
                return;
            }

            MeshCreatorBase creator = MeshCreatorBase.GetInstnace(configData);

            foreach (var sprite in sprites)
            {
                TextureInfo textureInfo = new TextureInfo(assetPath, sprite);
                creator.OverrideGeometry(sprite, textureInfo, configData);
            }
        }
    }
}
