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

        public enum ResizeMethod
        {
            Scale,
            AddAlphaOrCropArea
        }

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
            int oldGlobalMipMapLimit = QualitySettings.globalTextureMipmapLimit;
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
                    //force quality up global mipmap 
                    QualitySettings.globalTextureMipmapLimit = 0;

                    var assetImporter = AssetImporter.GetAtPath(assetPath);
                    var textureImporter = (TextureImporter)assetImporter;
                    var textureImporterSettings = new TextureImporterSettings();
                    textureImporter.ReadTextureSettings(textureImporterSettings);
                    var dummyTexture = TextureUtil.GetRawTexture(texture, textureImporter);
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
                    OutlineUtil.Resize(textureImporter, originalWidth, originalHeight, newWidth, newHeight, resizeMethod);
                    Debug.Log("Resized: " + assetPath, texture);
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError("Resize Error: " + assetPath, texture);
                    Debug.LogException(e);
                    return false;
                }
                finally
                {
                    //rollback global mipmap
                    QualitySettings.globalTextureMipmapLimit = oldGlobalMipMapLimit;
                }
            }

            return false;
        }

        //https://github.com/ababilinski/unity-gpu-texture-resize/blob/master/ResizeTool.cs
        public static Texture2D ScaleTexture(this Texture2D texture2D, int targetX, int targetY, bool mipmap = false, FilterMode filter = FilterMode.Bilinear)
        {
            RenderTexture rt = RenderTexture.GetTemporary(targetX, targetY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            RenderTexture.active = rt;
            Graphics.Blit(texture2D, rt);
            texture2D.Reinitialize(targetX, targetY, texture2D.format, mipmap);
            texture2D.filterMode = filter;

            try
            {
                texture2D.ReadPixels(new Rect(0.0f, 0.0f, targetX, targetY), 0, 0);
                texture2D.Apply();
            }
            catch
            {
                Debug.LogError("Read/Write is not enabled on texture " + texture2D.name);
            }

            RenderTexture.ReleaseTemporary(rt);
            return texture2D;
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

        public static bool ReplaceTextureValidate()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (string.IsNullOrEmpty(path) || AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
                return false;

            return true;
        }

        private static string sourcePath;
        
        //https://hacchi-man.hatenablog.com/entry/2020/01/28/220000
        public static void ReplaceTexture()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (string.IsNullOrEmpty(path) || AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
                return;
            
            string fullPath = Application.dataPath.Replace("/Assets", "/" + path);
            string fileName = Path.GetFileName(path);
            string ext = Path.GetExtension(path).Substring(1);
            sourcePath = EditorUtility.OpenFilePanel($"Select Replace Texture '{fileName}'", sourcePath, ext);

            if (string.IsNullOrEmpty(sourcePath))
                return;

            TextureImporter textureImporter =  (TextureImporter)AssetImporter.GetAtPath(path);
            if (textureImporter.TryGetRawImageSize(out int rawWidth, out int rawHeight) == false)
                return;

            if (TextureUtil.TryGetRawImageSize(sourcePath, out int newWidth, out int newHeight) == false)
                return;

            File.Copy(sourcePath, fullPath, true);
            AssetDatabase.Refresh(ImportAssetOptions.DontDownloadFromCacheServer);

            if (OutlineUtil.HasOutline(textureImporter) == false)
                return;

            if (rawWidth == newWidth && rawHeight == newHeight)
                return;
            
            TextureImporter newTextureImporter = (TextureImporter)AssetImporter.GetAtPath(path);

            if (EditorUtility.DisplayDialog("Resize Sprite Outline", "Source Texture has Sprite Outline.", "Scale Sprite Outline", "Do Nothing") == false)
                return;

            OutlineUtil.Resize(newTextureImporter, rawWidth, rawHeight, newWidth, newHeight, ResizeMethod.Scale);
        }
    }
}
