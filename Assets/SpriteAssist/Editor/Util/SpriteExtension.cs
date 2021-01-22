using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public struct Edge
    {
        public int t1;
        public int t2;
 
        public Edge(int t1, int t2)
        {
            this.t1 = t1;
            this.t2 = t2;
        }

        public Edge GetSwapped()
        {
            return new Edge(t2, t1);
        }
    }
 
    public static class SpriteExtension
    {
        public static void SetSpriteScaleToVertices(this Sprite sprite, Vector2[] vertices, float additionalScale, bool isFlipY, bool clamp)
        {
            float scaledPixelsPerUnit = sprite.pixelsPerUnit * additionalScale;
            Vector2 scaledPivot = sprite.pivot * additionalScale;
            Vector2 scaledSize = sprite.rect.size * additionalScale;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector2 vertex = vertices[i] * scaledPixelsPerUnit + scaledPivot;

                if (isFlipY)
                {
                    vertex.y = (vertex.y - scaledSize.y) * -1.0f;
                }

                if (clamp)
                {
                    vertex.x = Mathf.Clamp(vertex.x, 0, sprite.rect.size.x);
                    vertex.y = Mathf.Clamp(vertex.y, 0, sprite.rect.size.y);
                }

                vertices[i] = vertex;
            }
        }

        public static void GetVertexAndTriangle(this Sprite sprite, SpriteConfigData data, out Vector2[] vertices, out ushort[] triangles, MeshRenderType meshRenderType)
        {
            if (!TryVertexAndTriangle(sprite, data, out vertices, out triangles, meshRenderType))
            {
                //fallback
                vertices = sprite.vertices;
                triangles = sprite.triangles;
            }
        }

        public static string GetMeshAreaInfo(this Sprite sprite, Vector2[] vertices, ushort[] triangles)
        {
            float area = 0;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector2 a = vertices[triangles[i]];
                Vector2 b = vertices[triangles[i + 1]];
                Vector2 c = vertices[triangles[i + 2]];
                area += (a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y));
            }

            area *= 0.5f * sprite.pixelsPerUnit * sprite.pixelsPerUnit;
            area = Mathf.Abs(area);

            float meshAreaRatio = area / (sprite.rect.width * sprite.rect.height) * 100;
            return $"{vertices.Length} verts, {triangles.Length / 3} tris, {meshAreaRatio:F2}% overdraw";
        }

        private static bool TryVertexAndTriangle(Sprite sprite, SpriteConfigData data, out Vector2[] vertices, out ushort[] triangles, MeshRenderType meshRenderType)
        {
            vertices = Array.Empty<Vector2>();
            triangles = Array.Empty<ushort>();

            if (data == null || !data.overriden)
            {
                return false;
            }

            Vector2[][] paths = OutlineUtil.GenerateOutline(sprite, data, meshRenderType);

            TriangulationUtil.Triangulate(paths, data.edgeSmoothing, data.useNonZero, out vertices, out triangles);
            
            //validate
            if (vertices.Length >= ushort.MaxValue)
            {
                Debug.LogErrorFormat($"Too many veretics! Sprite '{sprite.name}' has {vertices.Length} vertices.");
                return false;
            }

            return true;
        }

        public static void UpdateMesh(this Sprite sprite, ref Mesh mesh, Vector2[] v, ushort[] tUShort)
        {
            int[] t = Array.ConvertAll(tUShort, i => (int)i);

            Vector3[] v2 = new Vector3[v.Length * 2];
            int[] t2 = new int[t.Length * 2];

            for (var i = 0; i < v.Length; i++)
            {
                v2[i + v.Length * 0] = new Vector3(v[i].x, v[i].y, -0.5f);
                v2[i + v.Length * 1] = new Vector3(v[i].x, v[i].y, 0.5f);
            }

            for (var i = 0; i < t.Length; i++)
            {
                //front
                t2[i + 0] = t[i + 0];

                //back(reversed)
                t2[t.Length * 2 - 1 - i] = t[i] + v.Length;
            }

            var edges = GetMeshEdges(t);
            List<int> t3 = new List<int>();

            foreach (var edge in edges)
            {
                t3.Add(edge.t2);
                t3.Add(edge.t1);
                t3.Add(edge.t1 + v.Length);

                t3.Add(edge.t1 + v.Length);
                t3.Add(edge.t2 + v.Length);
                t3.Add(edge.t2);
            }

            var temp = t2.ToList();
            temp.AddRange(t3);
            t2 = temp.ToArray();

            Vector2[] uv = new Vector2[v2.Length];

            for (var i = 0; i < uv.Length; i++)
            {
                uv[i] = new Vector2(v2[i].x, v2[i].y) * sprite.pixelsPerUnit + sprite.pivot;
                uv[i].x /= sprite.texture.width;
                uv[i].y /= sprite.texture.height;
            }

            Vector3[] normals = new Vector3[v2.Length];

            for (var i = 0; i < normals.Length; i++)
            {
                normals[i] = Vector3.back;
            }

            mesh.Clear();
            mesh.SetVertices(v2);
            mesh.SetUVs(0, uv);
            mesh.SetNormals(normals);
            mesh.SetTriangles(t2, 0);
        }
        
        private static Edge[] GetMeshEdges(int[] triangles)
        {
            HashSet<Edge> edges = new HashSet<Edge>();
            
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int t1 = triangles[i];
                int t2 = triangles[i + 1];
                int t3 = triangles[i + 2];

                AddOrRemoveEdge(edges, new Edge(t1, t2));
                AddOrRemoveEdge(edges, new Edge(t2, t3));
                AddOrRemoveEdge(edges, new Edge(t3, t1));
            }

            return edges.ToArray();
        }

        private static void AddOrRemoveEdge(HashSet<Edge> edges, Edge edge)
        {
            if (edges.Contains(edge))
            {
                edges.Remove(edge);
            }
            else if (edges.Contains(edge.GetSwapped()))
            {
                edges.Remove(edge.GetSwapped());
            }
            else
            {
                edges.Add(edge);
            }
        }

        public static GameObject CreateEmptyMeshPrefab(this Sprite sprite, bool hasSubObject)
        {
            string name = sprite.texture.name;
            GameObject instance = new GameObject(name);

            if (hasSubObject)
            {
                GameObject subInstance = new GameObject(name + "(sub)");
                subInstance.transform.SetParent(instance.transform);
            }

            string assetPath = AssetDatabase.GetAssetPath(sprite);
            string currentDirectory = Path.GetDirectoryName(assetPath);
            string path = Path.Combine(currentDirectory, name + ".prefab");
            GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(instance, path, InteractionMode.AutomatedAction);
            UnityEngine.Object.DestroyImmediate(instance);
            return prefab;
        }

        public static void AddComponentsAssets(this Sprite sprite, Vector2[] v, ushort[] t, GameObject prefab, string renderType, Shader shader)
        {
            //add components
            MeshFilter meshFilter = prefab.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = prefab.AddComponent<MeshRenderer>();

            //create new meshd
            Mesh mesh = new Mesh()
            {
                name = renderType,
            };

            sprite.UpdateMesh(ref mesh, v, t);
            meshFilter.mesh = mesh;

            //creat new material
            Material material = new Material(shader)
            {
                name = renderType,
                mainTexture = sprite.texture
            };
            meshRenderer.sharedMaterial = material;

            //set assets as sub-asset
            AssetDatabase.AddObjectToAsset(material, prefab);
            AssetDatabase.AddObjectToAsset(mesh, prefab);
            AssetDatabase.SaveAssets();
        }
    }
}
