using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpriteAssist
{
    public static class TextureUtil
    {
        private static readonly MethodInfo _getWidthAndHeight = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);

        public static bool TryGetRawImageSize(this TextureImporter importer, out int width, out int height)
        {
            if (_getWidthAndHeight != null)
            {
                object[] args = new object[] { 0, 0 };
                _getWidthAndHeight.Invoke(importer, args);

                width = (int)args[0];
                height = (int)args[1];

                return true;
            }

            height = width = 0;
            return false;
        }

        public static bool TryGetRawImageSize(string externalPath, out int width, out int height)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(externalPath);
                Texture2D newTexture = new Texture2D(2, 2);
                newTexture.LoadImage(bytes);
                width = newTexture.width;
                height = newTexture.height;
                Object.DestroyImmediate(newTexture);
                return true;

            }
            catch (Exception e)
            {
                width = 0;
                height = 0;
                Debug.LogException(e);
                return false;
            }
        }

        public static bool IsSingleSprite(this TextureImporterSettings textureImporterSettings)
        {
            return textureImporterSettings.spriteMode == 1;
        }

        public static void FixToSingleSprite(this TextureImporterSettings textureImporterSettings)
        {
            textureImporterSettings.spriteMode = 1;
        }

        public static Texture2D GetRawTexture(Texture2D texture, TextureImporter textureImporter)
        {
            string assetPath = AssetDatabase.GetAssetPath(texture);
            
            if (textureImporter.TryGetRawImageSize(out int rawWidth, out int rawHeight))
            {
                return GetRawTexture(assetPath, texture.name, texture.width, texture.height, rawWidth, rawHeight);
            }

            Debug.LogError("Original Image Size is wrong. Path: " + assetPath);
            return null;
        }

        public static Texture2D GetRawTexture(string assetPath, string name, int originalWidth, int originalHeight, int rawWidth, int rawHeight)
        {
            string projectPath = Path.GetDirectoryName(Application.dataPath);
            string fullPath = Path.Combine(projectPath, assetPath);
            byte[] bytes = File.ReadAllBytes(fullPath);
            Texture2D originalTexture = new Texture2D(rawWidth, rawHeight, TextureFormat.RGBA32, false);
            originalTexture.name = name;
            if (!originalTexture.LoadImage(bytes, false))
            {
                Object.DestroyImmediate(originalTexture);
                throw new InvalidOperationException($"[SpriteAssist] Failed to decode image. Path: {assetPath}");
            }

            if (originalTexture.width == originalWidth && originalTexture.height == originalHeight)
            {
                return originalTexture;
            }

            Texture2D resampledTexture = ResampleBilinear(originalTexture, originalWidth, originalHeight);
            resampledTexture.name = name;
            Object.DestroyImmediate(originalTexture);
            return resampledTexture;
        }

        // import 成果物の入力になるため、GPU（Blit / ReadPixels）を使わない決定的な CPU リサンプルを行う。
        public static Texture2D ResampleBilinear(Texture2D source, int targetWidth, int targetHeight)
        {
            if (targetWidth <= 0 || targetHeight <= 0)
            {
                throw new ArgumentOutOfRangeException($"Invalid resample size: {targetWidth}x{targetHeight}");
            }

            int sourceWidth = source.width;
            int sourceHeight = source.height;
            Color32[] sourcePixels = source.GetPixels32();
            Color32[] targetPixels = new Color32[targetWidth * targetHeight];
            float scaleX = (float)sourceWidth / targetWidth;
            float scaleY = (float)sourceHeight / targetHeight;

            for (int y = 0; y < targetHeight; y++)
            {
                float sy = (y + 0.5f) * scaleY - 0.5f;
                int y0 = Mathf.Clamp(Mathf.FloorToInt(sy), 0, sourceHeight - 1);
                int y1 = Mathf.Min(y0 + 1, sourceHeight - 1);
                float ty = Mathf.Clamp01(sy - y0);
                int row0 = y0 * sourceWidth;
                int row1 = y1 * sourceWidth;
                int targetRow = y * targetWidth;

                for (int x = 0; x < targetWidth; x++)
                {
                    float sx = (x + 0.5f) * scaleX - 0.5f;
                    int x0 = Mathf.Clamp(Mathf.FloorToInt(sx), 0, sourceWidth - 1);
                    int x1 = Mathf.Min(x0 + 1, sourceWidth - 1);
                    float tx = Mathf.Clamp01(sx - x0);
                    Color32 c00 = sourcePixels[row0 + x0];
                    Color32 c10 = sourcePixels[row0 + x1];
                    Color32 c01 = sourcePixels[row1 + x0];
                    Color32 c11 = sourcePixels[row1 + x1];

                    targetPixels[targetRow + x] = new Color32(
                        Lerp2D(c00.r, c10.r, c01.r, c11.r, tx, ty),
                        Lerp2D(c00.g, c10.g, c01.g, c11.g, tx, ty),
                        Lerp2D(c00.b, c10.b, c01.b, c11.b, tx, ty),
                        Lerp2D(c00.a, c10.a, c01.a, c11.a, tx, ty));
                }
            }

            Texture2D target = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
            target.filterMode = source.filterMode;
            target.wrapMode = source.wrapMode;
            target.SetPixels32(targetPixels);
            target.Apply(false, false);
            return target;
        }

        private static byte Lerp2D(byte c00, byte c10, byte c01, byte c11, float tx, float ty)
        {
            float top = c00 + (c10 - c00) * tx;
            float bottom = c01 + (c11 - c01) * tx;
            return (byte)Mathf.Clamp(Mathf.RoundToInt(top + (bottom - top) * ty), 0, 255);
        }
    }
}
