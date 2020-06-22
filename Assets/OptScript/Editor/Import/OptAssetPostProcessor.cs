using UnityEditor;
using UnityEngine;

namespace OptSprite
{
    public class OptAssetPostProcessor : AssetPostprocessor
    {
        private void OnPostprocessSprites(Texture2D texture, Sprite[] sprites)
        {
            TextureImporter textureImporter = assetImporter as TextureImporter;
            TextureImporterSettings textureImporterSettings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(textureImporterSettings);
            SpriteConfigData configData = SpriteConfigData.GetData(textureImporter.userData);

            if (textureImporterSettings.spriteMeshType != SpriteMeshType.Tight ||
                configData == null || !configData.overriden)
            {
                return;
            }

            MeshPrefabCreator creator = MeshPrefabCreator.GetInstnace(configData.mode);

            foreach (var sprite in sprites)
            {
                creator.OverrideGeometry(sprite, configData);
            }
        }
    }
}
