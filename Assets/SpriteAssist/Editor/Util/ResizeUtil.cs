using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public static class ResizeUtil
    {
        private const int MULTIPLE_OF_4 = 4;
        private const int SINGLE_LINE = 1;

        public enum ResizeMethod
        {
            Scale,
            AddAlphaOrCropArea
        }

        //public static void FindResizeTargetTexture()
        //{
        //    var targets = AssetDatabase.FindAssets(TextureAssetImporter.TEXTURE_TYPE, new[] { TextureAssetImporter.TARGET_DIRECTORY }).Select(AssetDatabase.GUIDToAssetPath).ToArray();

        //    foreach (var path in targets)
        //    {
        //        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        //        if (texture != null)
        //        {
        //            if (texture.width % MULTIPLE_OF_4 != 0 || texture.height % MULTIPLE_OF_4 != 0)
        //            {
        //                Debug.Log($"Wrong Size! {path}, Width: {texture.width}, Height:{texture.height}", texture);
        //            }
        //        }
        //        else
        //        {
        //            Debug.LogWarning($"Wrong Path! {path}");
        //        }
        //    }
        //}

        //public static void FindResizeTargetTextureForSingleLine()
        //{
        //    var targets = AssetDatabase.FindAssets(TextureAssetImporter.TEXTURE_TYPE, new[] { TextureAssetImporter.TARGET_DIRECTORY }).Select(AssetDatabase.GUIDToAssetPath).ToArray();

        //    foreach (var path in targets)
        //    {
        //        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        //        if (texture != null)
        //        {
        //            if (texture.width % MULTIPLE_OF_4 != 0 || texture.height % MULTIPLE_OF_4 != 0)
        //            {
        //                if (texture.width == 1 || texture.height == 1)
        //                {
        //                    Debug.Log($"Wrong Size + Single Line! {path}, Width: {texture.width}, Height:{texture.height}", texture);
        //                }
        //            }
        //        }
        //        else
        //        {
        //            Debug.LogWarning($"Wrong Path! {path}");
        //        }
        //    }
        //}

        public static void ResizeSelected(ResizeMethod method)
        {
            var count = 0;

            foreach (var obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
            {
                if (obj is Texture2D texture)
                {
                    var path = AssetDatabase.GetAssetPath(obj);
                    if (TryResizeForMultipleOf4(method, texture, path))
                    {
                        count++;
                    }
                }
                else if (obj is DefaultAsset defaultAsset)
                {
                    var path = AssetDatabase.GetAssetPath(defaultAsset);
                    count += ResizeAll(method, path);
                }
            }

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("ProjectShell", $"{count} Texture(s) resized.", "OK");
        }

        private static int ResizeAll(ResizeMethod method, string targetDirectory)
        {
            var count = 0;
            var targets = AssetDatabase.FindAssets("t:Texture", new[] { targetDirectory }).Select(AssetDatabase.GUIDToAssetPath).ToArray();

            foreach (var path in targets)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                if (texture != null && TryResizeForMultipleOf4(method, texture, path))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool TryResizeForMultipleOf4(ResizeMethod resizeMethod, Texture2D texture, string assetPath)
        {
            int originalWidth = texture.width;
            int originalHeight = texture.height;
            int additionalWidth = originalWidth % MULTIPLE_OF_4;
            int additionalHeight = originalHeight % MULTIPLE_OF_4;
            int reversedAdditionalWidth = MULTIPLE_OF_4 - additionalWidth;
            int reversedAdditionalHeight = MULTIPLE_OF_4 - additionalHeight;

            if (additionalWidth != 0 || additionalHeight != 0)
            {
                try
                {
                    var dummyTexture = SpriteAssist.TextureUtil.GetRawTexture(texture);
                    var assetImporter = AssetImporter.GetAtPath(assetPath);
                    var textureImporter = (TextureImporter)assetImporter;
                    var textureImporterSettings = new TextureImporterSettings();
                    textureImporter.ReadTextureSettings(textureImporterSettings);
                    var pivot = GetPivotValue((SpriteAlignment)textureImporterSettings.spriteAlignment, textureImporter.spritePivot);

                    int newWidth;
                    int newHeight;
                    Texture2D newTexture;

                    switch (resizeMethod)
                    {
                        case ResizeMethod.Scale:
                        {
                            additionalWidth = reversedAdditionalWidth <= additionalWidth ? -reversedAdditionalWidth : additionalWidth;
                            additionalHeight = reversedAdditionalHeight <= additionalHeight ? -reversedAdditionalHeight : additionalHeight;
                            newWidth = additionalWidth == 0 ? originalWidth : Mathf.Max(MULTIPLE_OF_4, originalWidth - additionalWidth);
                            newHeight = additionalHeight == 0 ? originalHeight : Mathf.Max(MULTIPLE_OF_4, originalHeight - additionalHeight);
                            newTexture = ScaleTexture(dummyTexture, newWidth, newHeight);
                            break;
                        }

                        case ResizeMethod.AddAlphaOrCropArea:
                        {
                            newWidth = additionalWidth == 0 ? originalWidth : Mathf.Max(MULTIPLE_OF_4, originalWidth + reversedAdditionalWidth);
                            newHeight = additionalHeight == 0 ? originalHeight : Mathf.Max(MULTIPLE_OF_4, originalHeight + reversedAdditionalHeight);
                            newTexture = AddAlphaAreaToTexture(dummyTexture, pivot, newWidth, newHeight);
                            break;
                        }

                        default:
                            throw new ArgumentOutOfRangeException(nameof(resizeMethod), resizeMethod, null);
                    }

                    File.WriteAllBytes(assetPath, newTexture.EncodeToPNG());
                    OutlineUtil.Resize(textureImporter, assetPath, originalWidth, originalHeight, newWidth, newHeight, resizeMethod);
                    Debug.Log("Resized: " + assetPath, texture);

                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError("Resize Error: " + assetPath, texture);
                    Debug.LogException(e);
                    return false;
                }
            }

            return false;
        }

        private static Texture2D ScaleTexture(Texture2D source, int newWidth, int newHeight)
        {
            var texColors = source.GetPixels();
            var newColors = new Color[newWidth * newHeight];
            var ratioX = 1.0f / ((float)newWidth / (source.width - 1));
            var ratioY = 1.0f / ((float)newHeight / (source.height - 1));

            for (var y = 0; y < newHeight; y++)
            {
                var yFloor = (int)Mathf.Floor(y * ratioY);
                var y1 = yFloor * source.width;
                var y2 = (yFloor + 1) * source.width;
                var yw = y * newWidth;

                for (var x = 0; x < newWidth; x++)
                {
                    int xFloor = (int)Mathf.Floor(x * ratioX);
                    var xLerp = x * ratioX - xFloor;
                    var a = Color.LerpUnclamped(texColors[y1 + xFloor], texColors[y1 + xFloor + 1], xLerp);
                    var b = Color.LerpUnclamped(texColors[y2 + xFloor], texColors[y2 + xFloor + 1], xLerp);
                    newColors[yw + x] = Color.LerpUnclamped(a, b, y * ratioY - yFloor);
                }
            }

            source.Resize(newWidth, newHeight);
            source.SetPixels(newColors);
            source.Apply();
            return source;
        }

        private static Texture2D AddAlphaAreaToTexture(Texture2D source, Vector2 pivot, int newWidth, int newHeight)
        {
            int startWidth = (int)((newWidth - source.width) * pivot.x);
            int startHeight = (int)((newHeight - source.height) * pivot.y);

            Texture2D background = new Texture2D(newWidth, newHeight);
            background.SetPixels32(new Color32[newWidth * newHeight]);

            for (int x = startWidth; x < background.width; x++)
            {
                for (int y = startHeight; y < background.height; y++)
                {
                    if (x - startWidth < source.width && y - startHeight < source.height)
                    {
                        var wmColor = source.GetPixel(x - startWidth, y - startHeight);
                        background.SetPixel(x, y, wmColor);
                    }
                }
            }

            background.Apply();
            return background;
        }

        private static Vector2 GetPivotValue(SpriteAlignment alignment, Vector2 customOffset)
        {
            switch (alignment)
            {
                case SpriteAlignment.BottomLeft:
                    return new Vector2(0f, 0f);

                case SpriteAlignment.BottomCenter:
                    return new Vector2(0.5f, 0f);

                case SpriteAlignment.BottomRight:
                    return new Vector2(1f, 0f);

                case SpriteAlignment.LeftCenter:
                    return new Vector2(0f, 0.5f);

                case SpriteAlignment.Center:
                    return new Vector2(0.5f, 0.5f);

                case SpriteAlignment.RightCenter:
                    return new Vector2(1f, 0.5f);

                case SpriteAlignment.TopLeft:
                    return new Vector2(0f, 1f);

                case SpriteAlignment.TopCenter:
                    return new Vector2(0.5f, 1f);

                case SpriteAlignment.TopRight:
                    return new Vector2(1f, 1f);

                case SpriteAlignment.Custom:
                    return customOffset;
            }

            return Vector2.zero;
        }
    }
}
