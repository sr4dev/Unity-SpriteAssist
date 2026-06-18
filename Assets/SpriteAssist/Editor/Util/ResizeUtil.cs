using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public static class ResizeUtil
    {
        private const int MULTIPLE_OF_4 = 4;
        private const int MAX_RECOMMENDED_SIZE = 2048;
        private const float SIZE_RATIO_TOLERANCE = 0.01f;
        private const float SAFE_CROP_ALPHA_THRESHOLD = 0.01f;
        private const float SAFE_CROP_COVERAGE_THRESHOLD = 0.001f;

        /// <summary>
        /// https://docs.unity3d.com/2022.2/Documentation/ScriptReference/QualitySettings-globalTextureMipmapLimit.html
        /// 'Global Mipmap Limit' affect to Resize Quality.
        private static int GlobalMipmap
        {
#if UNITY_2022_2_OR_NEWER

            get => QualitySettings.globalTextureMipmapLimit;
            set => QualitySettings.globalTextureMipmapLimit = value;
#else
            get => QualitySettings.masterTextureLimit;
            set => QualitySettings.masterTextureLimit = value;
#endif
        }

        public enum ResizeMethod
        {
            Scale,
            ExpandOrCrop
        }

        public enum SizeConstraint
        {
            MultipleOf4,
            PowerOfTwo
        }

        public static void ResizeSelected(ResizeMethod method, SizeConstraint constraint)
        {
            var count = 0;
            var logs = new List<string>();

            foreach (var obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
            {
                if (obj is Texture2D texture)
                {
                    var path = AssetDatabase.GetAssetPath(obj);
                    if (TryResize(constraint, method, texture, path, logs))
                    {
                        count++;
                    }
                }
                else if (obj is DefaultAsset defaultAsset)
                {
                    var path = AssetDatabase.GetAssetPath(defaultAsset);
                    count += ResizeAll(method, constraint, path, logs);
                }
            }

            AssetDatabase.Refresh();

            Debug.Log($"[SpriteAssist] Resize result ({constraint}, {method}): {count} texture(s) resized.\n{string.Join("\n", logs)}");
            EditorUtility.DisplayDialog("SpriteAssist", $"{count} Texture(s) resized.\nSee Console for details.", "OK");
        }

        private static int ResizeAll(ResizeMethod method, SizeConstraint constraint, string targetDirectory, List<string> logs)
        {
            var count = 0;
            var targets = AssetDatabase.FindAssets("t:Texture", new[] { targetDirectory }).Select(AssetDatabase.GUIDToAssetPath).ToArray();

            foreach (var path in targets)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                if (texture != null && TryResize(constraint, method, texture, path, logs))
                {
                    count++;
                }
            }

            return count;
        }

        private static int GetTargetSize(int originalSize, SizeConstraint constraint)
        {
            switch (constraint)
            {
                case SizeConstraint.MultipleOf4:
                {
                    int remainder = originalSize % MULTIPLE_OF_4;

                    if (remainder == 0)
                    {
                        return originalSize;
                    }

                    int target = remainder < MULTIPLE_OF_4 - remainder
                        ? originalSize - remainder
                        : originalSize + (MULTIPLE_OF_4 - remainder);
                    return Mathf.Max(MULTIPLE_OF_4, target);
                }

                case SizeConstraint.PowerOfTwo:
                {
                    int target = Mathf.ClosestPowerOfTwo(originalSize);
                    return Mathf.Max(MULTIPLE_OF_4, target);
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(constraint), constraint, null);
            }
        }

        private static bool TryResize(SizeConstraint constraint, ResizeMethod resizeMethod, Texture2D texture, string assetPath, List<string> logs)
        {
            int oldGlobalMipMapLimit = GlobalMipmap;
            int originalWidth = texture.width;
            int originalHeight = texture.height;
            int newWidth = GetTargetSize(originalWidth, constraint);
            int newHeight = GetTargetSize(originalHeight, constraint);

            if (newWidth == originalWidth && newHeight == originalHeight)
            {
                return false;
            }

            try
            {
                //force quality up global mipmap
                GlobalMipmap = 0;

                var assetImporter = AssetImporter.GetAtPath(assetPath);
                var textureImporter = (TextureImporter)assetImporter;
                var textureImporterSettings = new TextureImporterSettings();
                textureImporter.ReadTextureSettings(textureImporterSettings);
                var dummyTexture = TextureUtil.GetRawTexture(texture, textureImporter);
                var pivot = GetPivotValue((SpriteAlignment)textureImporterSettings.spriteAlignment, textureImporter.spritePivot);

                if (resizeMethod == ResizeMethod.ExpandOrCrop)
                {
                    newWidth = GetSafeCropSize(dummyTexture, true, newWidth, constraint, pivot, logs, assetPath);
                    newHeight = GetSafeCropSize(dummyTexture, false, newHeight, constraint, pivot, logs, assetPath);
                }

                Texture2D newTexture;

                switch (resizeMethod)
                {
                    case ResizeMethod.Scale:
                        newTexture = ScaleTexture(dummyTexture, newWidth, newHeight);
                        break;

                    case ResizeMethod.ExpandOrCrop:
                        newTexture = ExpandOrCropTexture(dummyTexture, pivot, newWidth, newHeight);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(resizeMethod), resizeMethod, null);
                }

                File.WriteAllBytes(assetPath, newTexture.EncodeToPNG());
                AdjustPixelsPerUnit(textureImporter, originalWidth, originalHeight, newWidth, newHeight);
                OutlineUtil.Resize(textureImporter, originalWidth, originalHeight, newWidth, newHeight, resizeMethod, pivot);

                string log = $"{assetPath}: {originalWidth}x{originalHeight} -> {newWidth}x{newHeight} (Pivot: {pivot})";
                logs?.Add(log);
                Debug.Log("Resized: " + log, texture);

                if (newWidth > MAX_RECOMMENDED_SIZE || newHeight > MAX_RECOMMENDED_SIZE)
                {
                    Debug.LogWarning($"Resized texture is larger than {MAX_RECOMMENDED_SIZE}px: {assetPath}", texture);
                }

                return true;
            }
            catch (Exception e)
            {
                logs?.Add($"{assetPath}: ERROR ({e.Message})");
                Debug.LogError("Resize Error: " + assetPath, texture);
                Debug.LogException(e);
                return false;
            }
            finally
            {
                //rollback global mipmap
                GlobalMipmap = oldGlobalMipMapLimit;
            }
        }

        private static void AdjustPixelsPerUnit(TextureImporter textureImporter, int oldWidth, int oldHeight, int newWidth, int newHeight)
        {
            if (textureImporter.textureType != TextureImporterType.Sprite || oldWidth == newWidth && oldHeight == newHeight)
                return;

            if (TryGetUniformScale(oldWidth, oldHeight, newWidth, newHeight, out float scale) == false)
                return;

            textureImporter.spritePixelsPerUnit *= scale;
            AssetDatabase.WriteImportSettingsIfDirty(textureImporter.assetPath);
        }

        private static int GetSafeCropSize(Texture2D texture, bool isWidth, int targetSize, SizeConstraint constraint, Vector2 pivot, List<string> logs, string assetPath)
        {
            int originalSize = isWidth ? texture.width : texture.height;

            if (targetSize >= originalSize)
                return targetSize;

            int cropPixels = originalSize - targetSize;
            int start = (int)(cropPixels * (isWidth ? pivot.x : pivot.y));

            if (CanCrop(texture, isWidth, start, cropPixels))
                return targetSize;

            int expandedSize = GetExpandedSize(originalSize, constraint);
            logs?.Add($"Expanded instead of cropping visible pixels: {assetPath} ({(isWidth ? "width" : "height")} {targetSize} -> {expandedSize})");
            return expandedSize;
        }

        private static bool CanCrop(Texture2D texture, bool isWidth, int start, int cropPixels)
        {
            int visiblePixels = 0;
            int totalPixels = cropPixels * (isWidth ? texture.height : texture.width);

            if (totalPixels <= 0)
                return true;

            if (start > 0 && HasVisiblePixels(texture, isWidth, 0, start, ref visiblePixels, totalPixels))
                return false;

            int endStart = (isWidth ? texture.width : texture.height) - (cropPixels - start);
            if (HasVisiblePixels(texture, isWidth, endStart, cropPixels - start, ref visiblePixels, totalPixels))
                return false;

            return true;
        }

        private static bool HasVisiblePixels(Texture2D texture, bool isWidth, int start, int length, ref int visiblePixels, int totalPixels)
        {
            if (length <= 0)
                return false;

            int end = start + length;
            int limit = isWidth ? texture.height : texture.width;

            for (int i = start; i < end; i++)
            {
                for (int j = 0; j < limit; j++)
                {
                    Color color = isWidth ? texture.GetPixel(i, j) : texture.GetPixel(j, i);
                    if (color.a <= SAFE_CROP_ALPHA_THRESHOLD)
                        continue;

                    visiblePixels++;
                    if ((float)visiblePixels / totalPixels > SAFE_CROP_COVERAGE_THRESHOLD)
                        return true;
                }
            }

            return false;
        }

        private static int GetExpandedSize(int originalSize, SizeConstraint constraint)
        {
            switch (constraint)
            {
                case SizeConstraint.MultipleOf4:
                    return Mathf.CeilToInt((float)originalSize / MULTIPLE_OF_4) * MULTIPLE_OF_4;

                case SizeConstraint.PowerOfTwo:
                    return Mathf.NextPowerOfTwo(originalSize);

                default:
                    throw new ArgumentOutOfRangeException(nameof(constraint), constraint, null);
            }
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

        private static Texture2D ExpandOrCropTexture(Texture2D source, Vector2 pivot, int newWidth, int newHeight)
        {
            int startWidth = Mathf.RoundToInt((newWidth - source.width) * pivot.x);
            int startHeight = Mathf.RoundToInt((newHeight - source.height) * pivot.y);

            Texture2D background = new Texture2D(newWidth, newHeight);
            background.SetPixels32(new Color32[newWidth * newHeight]);

            int minX = Mathf.Max(0, startWidth);
            int minY = Mathf.Max(0, startHeight);
            int maxX = Mathf.Min(background.width, startWidth + source.width);
            int maxY = Mathf.Min(background.height, startHeight + source.height);

            for (int x = minX; x < maxX; x++)
            {
                for (int y = minY; y < maxY; y++)
                {
                    var wmColor = source.GetPixel(x - startWidth, y - startHeight);
                    background.SetPixel(x, y, wmColor);
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

        public static bool ReplaceWithResizedTexturesValidate()
        {
            return Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets)
                .Any(obj => obj is Texture2D || obj is DefaultAsset);
        }

        private static string sourceDirectory;
        private static string sourcePath;

        public static void ReplaceWithResizedTextures()
        {
            var selectedObjects = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets);
            var selectedPaths = GetSelectedTexturePaths().ToArray();
            bool hasFolder = selectedObjects.Any(obj => obj is DefaultAsset);
            bool isSingleTexture = selectedPaths.Length == 1 && hasFolder == false;
            bool useFolder = false;
            Dictionary<string, string> replacements;

            if (isSingleTexture)
            {
                string targetFileName = Path.GetFileName(selectedPaths[0]);
                sourcePath = EditorUtility.OpenFilePanel($"Select resized texture for {targetFileName}", sourcePath, string.Empty);

                if (string.IsNullOrEmpty(sourcePath))
                    return;

                replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { targetFileName, sourcePath }
                };
            }
            else
            {
                string message = $"Select a resized texture file or folder for {selectedPaths.Length} selected texture(s). Folder subdirectories are included.";
                int sourceType = EditorUtility.DisplayDialogComplex("Replace with Resized Textures", message, "Select Folder", "Cancel", "Select File");

                if (sourceType == 1)
                    return;

                useFolder = sourceType == 0;

                if (useFolder)
                {
                    sourceDirectory = EditorUtility.OpenFolderPanel("Select Resized Textures Folder", sourceDirectory, string.Empty);

                    if (string.IsNullOrEmpty(sourceDirectory))
                        return;

                    replacements = GetReplacementFiles(sourceDirectory);
                }
                else
                {
                    sourcePath = EditorUtility.OpenFilePanel("Select Resized Texture", sourcePath, string.Empty);

                    if (string.IsNullOrEmpty(sourcePath))
                        return;

                    replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { Path.GetFileName(sourcePath), sourcePath }
                    };
                }
            }

            var logs = new List<string>();
            var replacedCount = 0;

            if (useFolder == false && selectedPaths.Length == 1)
            {
                if (ReplaceWithResizedTexture(selectedPaths[0], sourcePath, logs))
                    replacedCount++;

                AssetDatabase.Refresh(ImportAssetOptions.DontDownloadFromCacheServer);
                Debug.Log($"[SpriteAssist] Replace with Resized Textures: {replacedCount} texture(s) replaced.\n{string.Join("\n", logs)}");
                EditorUtility.DisplayDialog("SpriteAssist", $"{replacedCount} Texture(s) replaced.\nSee Console for details.", "OK");
                return;
            }

            foreach (string path in selectedPaths)
            {
                string fileName = Path.GetFileName(path);

                if (replacements.TryGetValue(fileName, out string source) == false)
                {
                    logs.Add($"Missing: {fileName}");
                    continue;
                }

                if (string.IsNullOrEmpty(source))
                {
                    logs.Add($"Ambiguous: {fileName}");
                    continue;
                }

                if (ReplaceWithResizedTexture(path, source, logs))
                    replacedCount++;
            }

            AssetDatabase.Refresh(ImportAssetOptions.DontDownloadFromCacheServer);
            Debug.Log($"[SpriteAssist] Replace with Resized Textures: {replacedCount} texture(s) replaced.\n{string.Join("\n", logs)}");
            EditorUtility.DisplayDialog("SpriteAssist", $"{replacedCount} Texture(s) replaced.\nSee Console for details.", "OK");
        }

        private static bool ReplaceWithResizedTexture(string path, string replacementPath, List<string> logs)
        {
            string fullPath = Application.dataPath.Replace("/Assets", "/" + path);
            TextureImporter textureImporter =  (TextureImporter)AssetImporter.GetAtPath(path);
            if (textureImporter.TryGetRawImageSize(out int rawWidth, out int rawHeight) == false)
            {
                logs?.Add($"Failed to read size: {path}");
                return false;
            }

            if (TextureUtil.TryGetRawImageSize(replacementPath, out int newWidth, out int newHeight) == false)
            {
                logs?.Add($"Failed to read size: {replacementPath}");
                return false;
            }

            bool sizeChanged = rawWidth != newWidth || rawHeight != newHeight;
            bool isUniformScale = TryGetUniformScale(rawWidth, rawHeight, newWidth, newHeight, out _);
            AdjustPixelsPerUnit(textureImporter, rawWidth, rawHeight, newWidth, newHeight);

            File.Copy(replacementPath, fullPath, true);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.DontDownloadFromCacheServer);
            logs?.Add($"Replaced: {path} ({rawWidth}x{rawHeight} -> {newWidth}x{newHeight})");

            if (sizeChanged && isUniformScale == false)
                logs?.Add($"Kept Pixel Per Unit because aspect ratio changed: {path}");

            if (sizeChanged == false || OutlineUtil.HasOutline(textureImporter) == false)
                return true;

            TextureImporter newTextureImporter = (TextureImporter)AssetImporter.GetAtPath(path);
            OutlineUtil.Resize(newTextureImporter, rawWidth, rawHeight, newWidth, newHeight, ResizeMethod.Scale);
            return true;
        }

        private static IEnumerable<string> GetSelectedTexturePaths()
        {
            var paths = new HashSet<string>();

            foreach (var obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
            {
                string path = AssetDatabase.GetAssetPath(obj);

                if (obj is Texture2D)
                {
                    if (paths.Add(path))
                        yield return path;
                    continue;
                }

                if (obj is DefaultAsset)
                {
                    foreach (string texturePath in AssetDatabase.FindAssets("t:Texture", new[] { path }).Select(AssetDatabase.GUIDToAssetPath))
                    {
                        if (paths.Add(texturePath))
                            yield return texturePath;
                    }
                }
            }
        }

        private static Dictionary<string, string> GetReplacementFiles(string directory)
        {
            var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string path in Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(path);

                if (replacements.ContainsKey(fileName))
                {
                    replacements[fileName] = string.Empty;
                    continue;
                }

                replacements.Add(fileName, path);
            }

            return replacements;
        }

        private static bool TryGetUniformScale(int oldWidth, int oldHeight, int newWidth, int newHeight, out float scale)
        {
            float widthScale = (float)newWidth / oldWidth;
            float heightScale = (float)newHeight / oldHeight;
            float maxScale = Mathf.Max(widthScale, heightScale);
            scale = (widthScale + heightScale) * 0.5f;
            return Mathf.Abs(widthScale - heightScale) / maxScale <= SIZE_RATIO_TOLERANCE;
        }
    }
}
