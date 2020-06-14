using UnityEditor;
using UnityEngine;

namespace OptSprite
{
    public class SpriteAssetPostProcessor : AssetPostprocessor
    {
        private void OnPostprocessSprites(Texture2D texture, Sprite[] sprites)
        {
            TextureImporter textureImporter = assetImporter as TextureImporter;
            OptSpriteData configData = OptSpriteData.GetData(textureImporter.userData);
            TextureImporterSettings textureImporterSettings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(textureImporterSettings);

            if (configData == null ||
                configData.overriden == false || 
                textureImporterSettings.spriteMeshType  == SpriteMeshType.FullRect ||
                textureImporterSettings.spriteMode != 1)
            {
                return;
            }

            foreach (var sprite in sprites)
            {
                SpriteUtil.GetMeshData(sprite, configData, out var vertices, out var triangles);
                SpriteUtil.GetScaledVertices(vertices, sprite.pixelsPerUnit, sprite.pivot, sprite.rect.size, 1, false, false);
                sprite.OverrideGeometry(vertices, triangles);
            }
        }
    }
}
