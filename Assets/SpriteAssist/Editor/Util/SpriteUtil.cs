using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public static class SpriteUtil
    {
        public static Vector2 GetNormalizedPivot(this Sprite sprite)
        {
            return sprite.pivot / sprite.rect.size;
        }

        public static void GetVertexAndTriangle2D(this Sprite sprite, SpriteConfigData configData, out Vector2[] vertices2D, out ushort[] triangles2D, MeshRenderType meshRenderType)
        {
            if (!TryGetVertexAndTriangle2D(sprite, configData, out vertices2D, out triangles2D, meshRenderType))
            {
                //fallback
                vertices2D = sprite.vertices;
                triangles2D = sprite.triangles;
            }
        }

        public static void GetVertexAndTriangle3D(this Sprite sprite, SpriteConfigData configData, out Vector3[] vertices3D, out int[] triangles3D, MeshRenderType meshRenderType)
        {
            if (!TryGetVertexAndTriangle2D(sprite, configData, out var vertices2D, out var triangles2D, meshRenderType))
            {
                //fallback
                vertices2D = sprite.vertices;
                triangles2D = sprite.triangles;
            }

            vertices3D = Array.ConvertAll(vertices2D, i => new Vector3(i.x, i.y, 0));
            triangles3D = Array.ConvertAll(triangles2D, i => (int)i);

            if (configData.thickness > 0)
            {
                TriangulationUtil.ExpandMeshThickness(ref vertices3D, ref triangles3D, configData.thickness);
            }
        }

        public static bool TryGetVertexAndTriangle2D(this Sprite sprite, SpriteConfigData configData, out Vector2[] vertices, out ushort[] triangles, MeshRenderType meshRenderType)
        {
            vertices = Array.Empty<Vector2>();
            triangles = Array.Empty<ushort>();

            if (configData == null || sprite == null ||
                configData.mode == SpriteConfigData.Mode.UnityDefaultForTransparent ||
                configData.mode == SpriteConfigData.Mode.UnityDefaultForOpaque)
            {
                return false;
            }

            Vector2[][] paths = OutlineUtil.GenerateOutline(sprite, configData, meshRenderType);

            if (meshRenderType == MeshRenderType.Grid)
            {
                vertices = paths[0];
                triangles = new ushort[vertices.Length];

                for (var i = 0; i < triangles.Length; i++)
                {
                    triangles[i] = (ushort)i;
                }
            }
            else
            {
                TriangulationUtil.Triangulate(paths, configData.edgeSmoothing, configData.useNonZero, out vertices, out triangles);
            }

            //validate
            if (vertices.Length >= ushort.MaxValue)
            {
                Debug.LogErrorFormat($"Too many vertices! Sprite '{sprite.name}' has {vertices.Length} vertices.");
                return false;
            }

            return true;
        }
        
        public static Sprite CreateDummySprite(Sprite originalSprite, string assetPath)
        {
            string name = originalSprite.texture.name;
            int width = originalSprite.texture.width;
            int height = originalSprite.texture.height;
            Vector2 pivot = originalSprite.GetNormalizedPivot();
            Rect rect = new Rect(0, 0, width, height);
            float pixelsPerUnit = originalSprite.pixelsPerUnit;

            Texture2D rawTexture = TextureUtil.GetRawTexture(assetPath, name, width, height);
            Sprite newSprite = Sprite.Create(rawTexture, rect, pivot, pixelsPerUnit);
            newSprite.name = name + "(Dummy Sprite)";
            return newSprite;
        }

        public static void AddAlphaArea(Sprite source, string assetPath)
        {
            if (IsPowerOfTwo(source.texture.width) && IsPowerOfTwo(source.texture.height))
            {
                return;
            }

            int width = HighestPowerOf2(source.texture.width);
            int height = HighestPowerOf2(source.texture.height);

            Vector2 pivot = source.GetNormalizedPivot();

            int startWidth = (int)((width - source.texture.width) * pivot.x);
            int startHeight = (int)((height - source.texture.height) * pivot.y);
            
            var tex = new Texture2D(width, height);
            tex.SetPixels(new Color[width * height]);
            tex = MergeTexture(tex, source.texture, startWidth, startHeight);

            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            AssetDatabase.Refresh();
        }

        public static int NearestPowerOf2(int n)
        {
            int res = 0;
            for (int i = n; i >= 1; i--)
            {
                // If i is a power of 2
                if ((i & (i - 1)) == 0)
                {
                    res = i;
                    break;
                }
            }
            return res;
        }

        public static int HighestPowerOf2(int n)
        {
            int power = 1;
            while (power < n)
                power <<= 1;
            return power;
        }

        public static bool IsPowerOfTwo(int x)
        {
            return (x & (x - 1)) == 0;
        }

        public static Texture2D MergeTexture(Texture2D background, Texture2D source, int startPositionX, int startPositionY)
        {
            for (int x = startPositionX; x < background.width; x++)
            {
                for (int y = startPositionY; y < background.height; y++)
                {
                    if (x - startPositionX < source.width && y - startPositionY < source.height)
                    {
                        var wmColor = source.GetPixel(x - startPositionX, y - startPositionY);

                        //premultiplied alpha
                        wmColor.r *= wmColor.a;
                        wmColor.g *= wmColor.a;
                        wmColor.b *= wmColor.a;
                        background.SetPixel(x, y, wmColor);
                    }
                }
            }

            background.Apply();
            return background;
        }

        public static Sprite FindSprite(UnityEngine.Object target)
        {
            switch (target)
            {
                case Sprite s:
                    return s;

                case GameObject go:
                    if (go.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
                    {
                        return spriteRenderer.sprite;
                    }
                    else if (go.TryGetComponent<MeshRenderer>(out var meshRenderer))
                    {
                        var path = AssetDatabase.GetAssetPath(meshRenderer.sharedMaterial.mainTexture);
                        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    }
                    break;
            }

            return null;
        }

    }

}
