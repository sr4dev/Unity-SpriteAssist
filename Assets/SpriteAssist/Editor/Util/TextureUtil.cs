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
            Texture2D originalTexture = new Texture2D(rawWidth, rawHeight);
            originalTexture.name = name;
            originalTexture.LoadImage(bytes);
            return originalTexture.ScaleTexture(originalWidth, originalHeight);
        }
    }
}