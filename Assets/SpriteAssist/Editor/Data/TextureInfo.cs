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

        private TextureInfo(string path, float ppu, Vector2 normalizedPivot, Vector2 size)
        {
            textureAssetPath = path; pixelPerUnit = ppu; pivot = normalizedPivot * size;
            rect = new Rect(Vector2.zero, size);
        }

        // Saved outlines use pixels of the source image, not Sprite PPU/rect after platform downscaling.
        public static TextureInfo ForSourceImage(Sprite sprite, string path)
        {
            if (!(UnityEditor.AssetImporter.GetAtPath(path) is UnityEditor.TextureImporter importer) || !importer.TryGetRawImageSize(out int width, out int height))
                throw new SpriteAssistGeometryException("Source texture dimensions unavailable: " + path);
            Vector2 normalizedPivot;
            if (sprite != null) normalizedPivot = sprite.GetNormalizedPivot();
            else
            {
                // Allow recovery preflight even when corrupt legacy input prevents the Sprite itself from importing.
                var settings = new UnityEditor.TextureImporterSettings(); importer.ReadTextureSettings(settings);
                normalizedPivot = settings.spriteAlignment switch
                {
                    1 => new Vector2(0, 1), 2 => new Vector2(.5f, 1), 3 => Vector2.one,
                    4 => new Vector2(0, .5f), 5 => new Vector2(1, .5f),
                    6 => Vector2.zero, 7 => new Vector2(.5f, 0), 8 => new Vector2(1, 0),
                    9 => settings.spritePivot, _ => new Vector2(.5f, .5f)
                };
            }
            return new TextureInfo(path, importer.spritePixelsPerUnit, normalizedPivot, new Vector2(width, height));
        }
    }
}
