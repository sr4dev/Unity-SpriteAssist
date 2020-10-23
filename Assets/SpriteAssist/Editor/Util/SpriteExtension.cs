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

        public static void GetMeshData(this Sprite sprite, SpriteConfigData data, out Vector2[] vertices, out ushort[] triangles, MeshRenderType meshRenderType)
        {
            if (!TryGetMeshData(sprite, data, out vertices, out triangles, meshRenderType))
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
            return $"Verts: {vertices.Length}  Tris: {triangles.Length}  Fillrate: {meshAreaRatio:F2}%";
        }

        private static bool TryGetMeshData(Sprite sprite, SpriteConfigData data, out Vector2[] vertices, out ushort[] triangles, MeshRenderType meshRenderType)
        {
            vertices = Array.Empty<Vector2>();
            triangles = Array.Empty<ushort>();

            if (data == null || !data.overriden)
            {
                return false;
            }

            Vector2[][] paths = OutlineUtil.GenerateOutline(sprite, data, meshRenderType);
            TriangulationUtil.Triangulate(paths, data.edgeSmoothing, data.windingRule, out vertices, out triangles);

            //validate
            if (vertices.Length >= ushort.MaxValue)
            {
                Debug.LogErrorFormat($"Too many veretics! Sprite '{sprite.name}' has {vertices.Length} vertices.");
                return false;
            }

            return true;
        }

        public static void UpdateMesh(this Sprite sprite, ref Mesh mesh, Vector2[] v, ushort[] t)
        {
            Vector2[] uv = new Vector2[v.Length];

            for (var i = 0; i < uv.Length; i++)
            {
                uv[i] = v[i] * sprite.pixelsPerUnit + sprite.pivot;
                uv[i].x /= sprite.texture.width;
                uv[i].y /= sprite.texture.height;
            }

            mesh.Clear();
            mesh.SetVertices(Array.ConvertAll(v, i => (Vector3)i));
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(Array.ConvertAll(t, i => (int)i), 0);
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
