using UnityEngine;

namespace SpriteAssist
{
    public static class MeshUtil
    {
        public static Vector2[] GetScaledVertices(Vector2[] vertices, TextureInfo textureInfo, float additionalScale = 1, bool isFlipY = false, bool isClamped = false)
        {
            float scaledPixelsPerUnit = textureInfo.pixelPerUnit * additionalScale;
            Vector2 scaledPivot = textureInfo.pivot * additionalScale;
            Vector2 scaledSize = textureInfo.rect.size * additionalScale;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector2 vertex = vertices[i] * scaledPixelsPerUnit + scaledPivot;

                if (isFlipY)
                {
                    vertex.y = (vertex.y - scaledSize.y) * -1.0f;
                }

                if (isClamped)
                {
                    vertex.x = Mathf.Clamp(vertex.x, 0, textureInfo.rect.size.x);
                    vertex.y = Mathf.Clamp(vertex.y, 0, textureInfo.rect.size.y);
                }

                vertices[i] = vertex;
            }

            return vertices;
        }

        public static string GetAreaInfo(Vector2[] vertices2D, ushort[] triangles2D, TextureInfo textureInfo)
        {
            float area = 0;

            for (int i = 0; i < triangles2D.Length; i += 3)
            {
                Vector2 a = vertices2D[triangles2D[i]];
                Vector2 b = vertices2D[triangles2D[i + 1]];
                Vector2 c = vertices2D[triangles2D[i + 2]];
                area += (a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y));
            }

            area *= 0.5f * textureInfo.pixelPerUnit * textureInfo.pixelPerUnit;
            area = Mathf.Abs(area);

            float meshAreaRatio = area / (textureInfo.rect.size.x * textureInfo.rect.size.y) * 100;
            return $"{vertices2D.Length} verts, {triangles2D.Length / 3} tris, {meshAreaRatio:F2}% overdraw";
        }

        public static void Update(this Mesh mesh, Vector3[] v, int[] t, TextureInfo textureInfo)
        {
            Vector2[] uv = new Vector2[v.Length];

            for (var i = 0; i < uv.Length; i++)
            {
                uv[i] = new Vector2(v[i].x, v[i].y) * textureInfo.pixelPerUnit + textureInfo.pivot;
                uv[i].x /= textureInfo.rect.size.x;
                uv[i].y /= textureInfo.rect.size.y;
            }

            mesh.Clear();
            mesh.SetVertices(v);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}