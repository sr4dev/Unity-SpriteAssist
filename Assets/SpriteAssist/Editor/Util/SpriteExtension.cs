using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
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

        public static string GetMeshAreaInfo(this Sprite sprite, Vector2[] vertices2D, ushort[] triangles2D)
        {
            float area = 0;

            for (int i = 0; i < triangles2D.Length; i += 3)
            {
                Vector2 a = vertices2D[triangles2D[i]];
                Vector2 b = vertices2D[triangles2D[i + 1]];
                Vector2 c = vertices2D[triangles2D[i + 2]];
                area += (a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y));
            }

            area *= 0.5f * sprite.pixelsPerUnit * sprite.pixelsPerUnit;
            area = Mathf.Abs(area);

            float meshAreaRatio = area / (sprite.rect.width * sprite.rect.height) * 100;
            return $"{vertices2D.Length} verts, {triangles2D.Length / 3} tris, {meshAreaRatio:F2}% overdraw";
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
                Debug.LogErrorFormat($"Too many veretics! Sprite '{sprite.name}' has {vertices.Length} vertices.");
                return false;
            }

            return true;
        }

        public static void UpdateMesh(this Sprite sprite, ref Mesh mesh, Vector3[] v, int[] t)
        {
            Vector2[] uv = new Vector2[v.Length];

            for (var i = 0; i < uv.Length; i++)
            {
                uv[i] = new Vector2(v[i].x, v[i].y) * sprite.pixelsPerUnit + sprite.pivot;
                uv[i].x /= sprite.texture.width;
                uv[i].y /= sprite.texture.height;
            }

            mesh.Clear();
            mesh.SetVertices(v);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
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

        public static void AddComponentsAssets(this Sprite sprite, Vector3[] v, int[] t, GameObject prefab, string renderType, string shaderName)
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
            Material material = new Material(Shader.Find(shaderName))
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
