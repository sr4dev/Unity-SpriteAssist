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
                    object[] args = new object[] {0, 0};
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

        public static Texture2D GetOriginalTexture(Texture2D texture)
        {
            string projectPath = Path.GetDirectoryName(Application.dataPath);
            string assetPath = AssetDatabase.GetAssetPath(texture);
            string fullPath = Path.Combine(projectPath, assetPath);
            byte[] bytes = File.ReadAllBytes(fullPath);
            Texture2D originalTexture = new Texture2D(texture.width, texture.height);
            originalTexture.name = texture.name;
            originalTexture.LoadImage(bytes);
            return originalTexture;
        }
    }
}
