using System.Collections.Generic;
using System.Linq;
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

        public static void Update(this Mesh mesh, Vector3[] v, int[] t, TextureInfo textureInfo, bool splitVertices)
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

            if (splitVertices)
            {
                mesh.SplitVertices();
            }
        }

        //reference 'https://github.com/sr4dev/Unity-SpriteAssist/issues/38 by MateuszRe'
        public static void SplitVertices(this Mesh mesh)
        {
            var vertices = mesh.vertices.ToList();
            var vCnt = vertices.Count;
            if (vCnt == 0)
                return;

            var uvs = mesh.uv.ToList();
            var colors = mesh.colors.ToList();
            var tangents = mesh.tangents.ToList();

            var hasUvs = vCnt == uvs.Count;
            var hasColors = vCnt == colors.Count;
            var hasTangents = vCnt == tangents.Count;

            var unique = new HashSet<int>();
            var allTriangles = new List<int[]>();

            var subMeshCount = mesh.subMeshCount;

            for (int s = 0; s < subMeshCount; s++)
            {
                var triangles = mesh.GetTriangles(s);
                int tCnt = triangles.Length;
                
                for (int t = 0; t < tCnt; t++)
                {
                    int index = triangles[t];
                    if (unique.Contains(index))
                    {
                        vertices.Add(vertices[index]);
                        if (hasUvs)
                            uvs.Add(uvs[index]);
                        if (hasColors)
                            colors.Add(colors[index]);
                        if (hasTangents)
                            tangents.Add(tangents[index]);

                        triangles[t] = vertices.Count - 1;
                    }
                    else
                        unique.Add(index);
                }

                allTriangles.Add(triangles);
            }

            mesh.SetVertices(vertices);
            for (int s = 0; s < subMeshCount; s++)
                mesh.SetTriangles(allTriangles[s], s);

            if (hasUvs)
                mesh.SetUVs(0, uvs);
            if (hasColors)
                mesh.SetColors(colors);
            if (hasTangents)
                mesh.SetTangents(tangents);

            mesh.RecalculateNormals();
        }

        //reference 'https://github.com/sr4dev/Unity-SpriteAssist/issues/38 by MateuszRe'
        public static void WeldVertices(Mesh mesh, float threshold = 0.005f, float bucketStep = 3, bool recalculateNormals = true, bool ignoreNormals = false, bool printResults = true)
        {
            int oldCnt = mesh.vertices.Length;

            if (oldCnt == 0) return;

            mesh.RecalculateBounds();
            if (recalculateNormals) mesh.RecalculateNormals();

            var vertBuffer = new List<Vector3>(8192);
            var normalBuffer = new List<Vector3>(8192);
            var colorBuffer = new List<Color>(8192);
            var uvsBuffer = new List<Vector2>(8192);
            var trisBuffer = new List<int>();
            var indexBuffer = new List<int>();

            var vertices = mesh.vertices;
            var colors = mesh.colors;
            var normals = mesh.normals;
            var uvs = mesh.uv;
            int[] triangles = mesh.triangles;

            bool hasColors = colors.Length == vertices.Length;
            bool hasUVs = uvs.Length == vertices.Length;
            bool hasNormals = normals.Length == vertices.Length;
            ignoreNormals |= !hasNormals;

            int newSize = 0;

            Vector3 min = mesh.bounds.min;
            Vector3 max = mesh.bounds.max;

            int bucketSizeX = Mathf.FloorToInt((max.x - min.x) / bucketStep) + 1;
            int bucketSizeY = Mathf.FloorToInt((max.y - min.y) / bucketStep) + 1;
            int bucketSizeZ = Mathf.FloorToInt((max.z - min.z) / bucketStep) + 1;

            int size = bucketSizeX * bucketSizeY * bucketSizeZ;
            List<int>[] buckets = new List<int>[size];

            int x, y, z;
            int tempIndex;
            List<int> tempList;
            for (int i = 0; i < oldCnt; i++)
            {
                x = Mathf.Max(0, Mathf.Min(Mathf.FloorToInt((vertices[i].x - min.x) / bucketStep), bucketSizeX - 1));
                y = Mathf.Max(0, Mathf.Min(Mathf.FloorToInt((vertices[i].y - min.y) / bucketStep), bucketSizeY - 1));
                z = Mathf.Max(0, Mathf.Min(Mathf.FloorToInt((vertices[i].z - min.z) / bucketStep), bucketSizeZ - 1));

                tempIndex = x * bucketSizeZ * bucketSizeY + y * bucketSizeZ + z;

                if ((object)buckets[tempIndex] == null)
                {
                    tempList = new List<int>(5);
                    buckets[tempIndex] = tempList;
                }
                else tempList = buckets[tempIndex];

                for (int j = 0; j < tempList.Count; j++)
                {
                    tempIndex = tempList[j];
                    if (Vector3.SqrMagnitude(vertBuffer[tempIndex] - vertices[i]) < threshold
                                            && (!hasUVs || uvs[i] == uvsBuffer[tempIndex])
                                            && (!hasColors || colors[i] == colorBuffer[tempIndex])
                                            && (ignoreNormals || normals[i] == normalBuffer[tempIndex]))
                    {
                        indexBuffer.Add(tempList[j]);
                        goto skip;
                    }
                }

                vertBuffer.Add(vertices[i]);
                if (hasColors)
                    colorBuffer.Add(colors[i]);
                if (hasUVs)
                    uvsBuffer.Add(uvs[i]);
                if (!ignoreNormals)
                    normalBuffer.Add(normals[i]);

                tempList.Add(newSize);
                indexBuffer.Add(newSize);
                newSize++;

                skip:;
            }

            for (int i = 0; i < triangles.Length; i++)
            {
                trisBuffer.Add(indexBuffer[triangles[i]]);
            }

            mesh.Clear();

            mesh.SetVertices(vertBuffer);

            if (hasColors)
                mesh.SetColors(colorBuffer);
            if (hasUVs)
                mesh.SetUVs(0, uvsBuffer);

            mesh.SetTriangles(trisBuffer, 0, true);

            if (!ignoreNormals) mesh.SetNormals(normalBuffer);
            else mesh.RecalculateNormals();

            if (printResults) Debug.Log("Reduced " + mesh.name + " from " + vertices.Length + " to " + vertBuffer.Count);
        }

    }
}