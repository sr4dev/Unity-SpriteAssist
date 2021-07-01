using UnityEngine;

namespace SpriteAssist
{
    public struct TextureInfo
    {
        public string textureAssetPath;
        public string textureName;
        public string spriteName;
        public float pixelPerUnit;
        public Vector2 pivot;
        public Vector2 normalizedPivot;
        public Rect rect;

        public TextureInfo(Sprite sprite, string originalAssetPath)
        {
            textureAssetPath = originalAssetPath;
            textureName = sprite.texture.name;
            spriteName = sprite.name;
            pixelPerUnit = sprite.pixelsPerUnit;
            pivot = sprite.pivot;
            normalizedPivot = sprite.GetNormalizedPivot();
            rect = sprite.rect;
        }
    }
}