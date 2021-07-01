using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public static class TextureUtil
    {
        public static bool GetOriginalImageSize(this Texture2D asset, out int width, out int height)
        {
            if (asset != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(asset);
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

                if (importer != null)
                {
                    object[] args = new object[] { 0, 0 };
                    MethodInfo mi = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
                    mi.Invoke(importer, args);

                    width = (int)args[0];
                    height = (int)args[1];

                    return true;
                }
            }

            height = width = 0;
            return false;
        }

        public static Texture2D GetRawTexture(Texture2D texture)
        {
            string assetPath = AssetDatabase.GetAssetPath(texture);
            return GetRawTexture(assetPath, texture.name, texture.width, texture.height);
        }

        public static Texture2D GetRawTexture(string assetPath, string name, int width, int height)
        {
            string projectPath = Path.GetDirectoryName(Application.dataPath);
            string fullPath = Path.Combine(projectPath, assetPath);
            byte[] bytes = File.ReadAllBytes(fullPath);
            Texture2D originalTexture = new Texture2D(width, height);
            originalTexture.name = name;
            originalTexture.LoadImage(bytes);
            return originalTexture;
        }
    }
}