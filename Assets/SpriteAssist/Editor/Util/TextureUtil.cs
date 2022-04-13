using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public static class TextureUtil
    {
        public static bool TryGetRawImageSize(this Texture2D asset, TextureImporter importer, out int width, out int height)
        {
            if (asset != null)
            {
                object[] args = new object[] { 0, 0 };
                MethodInfo mi = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
                mi.Invoke(importer, args);

                width = (int)args[0];
                height = (int)args[1];

                return true;
            }

            height = width = 0;
            return false;
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
            
            if (texture.TryGetRawImageSize(textureImporter, out int rawWidth, out int rawHeight))
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
            Texture2D originalTexture = new Texture2D(rawWidth, rawHeight);
            originalTexture.name = name;
            originalTexture.LoadImage(bytes);
            return originalTexture.ScaleTexture(originalWidth, originalHeight);
        }
    }
}