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

        private static bool TryGetVertexAndTriangle2D(Sprite sprite, SpriteConfigData configData, out Vector2[] vertices, out ushort[] triangles, MeshRenderType meshRenderType)
        {
            vertices = Array.Empty<Vector2>();
            triangles = Array.Empty<ushort>();

            if (configData == null || !configData.IsOverriden)
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

        public static Sprite CreateDummySprite(Texture2D texture)
        {
            return CreateDummySprite(texture, new Vector2(0.5f, 0.5f), 100);
        }

        public static Sprite CreateDummySprite(Texture2D texture, Vector3 pivot, float pixelsPerUnit)
        {
            Texture2D originalTexture = TextureUtil.GetOriginalTexture(texture);

            //texture.GetOriginalImageSize(out int width, out int height);
            Rect rect = new Rect(0, 0, originalTexture.width, originalTexture.height);
            Sprite sprite = Sprite.Create(originalTexture, rect, pivot, pixelsPerUnit);
            sprite.name = originalTexture.name + "(Dummy Sprite)";
            return sprite;
        }
    }
}
