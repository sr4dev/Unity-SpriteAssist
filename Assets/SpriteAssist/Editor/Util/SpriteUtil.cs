using System;
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
            TriangulationUtil.Triangulate(paths, configData.edgeSmoothing, configData.useNonZero, out vertices, out triangles);

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


    }

}
