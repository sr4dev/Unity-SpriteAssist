using UnityEngine;

namespace SpriteAssist
{
    public struct TextureInfo
    {
        public readonly string textureAssetPath;
        public readonly float pixelPerUnit;
        public readonly Vector2 pivot;
        public readonly Rect rect;

        public TextureInfo(Sprite sprite, string originalAssetPath)
        {
            textureAssetPath = originalAssetPath;
            pixelPerUnit = sprite.pixelsPerUnit;
            pivot = sprite.pivot;
            rect = sprite.rect;
        }
    }
}