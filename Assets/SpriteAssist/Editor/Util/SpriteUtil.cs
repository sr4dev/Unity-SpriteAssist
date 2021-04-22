using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpriteAssist
{
    public struct TextureInfo
    {
        public string textureAssetPath;
        public string textureName;
        public string spriteName;
        public float pixelPerUnit;
        public Vector2 pivot;
        public Vector2 normalizedPivot;
        public Rect rect;

        public TextureInfo(string originalAssetPath, Sprite sprite)
        {
            textureAssetPath = originalAssetPath;
            textureName = sprite.texture.name;
            spriteName = sprite.name;
            pixelPerUnit = sprite.pixelsPerUnit;
            pivot = sprite.pivot;
            normalizedPivot = sprite.pivot / sprite.rect.size;
            rect = sprite.rect;
        }
    }

    public static class MeshUtil
    {
        public static Vector2[] GetScaledVertices(Vector2[] vertices, TextureInfo textureInfo, float additionalScale = 1, bool isFlipY = false)
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

                //vertex.x = Mathf.Clamp(vertex.x, 0, textureScaleInfo.rect.size.x);
                //vertex.y = Mathf.Clamp(vertex.y, 0, textureScaleInfo.rect.size.y);

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

    public static class PrefabUtil
    {
        public static GameObject CreateMeshPrefab(TextureInfo textureInfo, bool hasSubObject)
        {
            GameObject instance = new GameObject(textureInfo.textureName);

            if (hasSubObject)
            {
                GameObject subInstance = new GameObject(textureInfo.textureName + "(sub)");
                subInstance.transform.SetParent(instance.transform);
            }

            string currentDirectory = Path.GetDirectoryName(textureInfo.textureAssetPath);
            string path = Path.Combine(currentDirectory, textureInfo.textureName + ".prefab");
            GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(instance, path, InteractionMode.AutomatedAction);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        public static GameObject UpdateMeshPrefab(TextureInfo textureInfo, bool hasSubObject, string oldPrefabPath)
        {
            if (string.IsNullOrEmpty(oldPrefabPath))
            {
                return CreateMeshPrefab(textureInfo, hasSubObject);
            }
        
            GameObject instance = PrefabUtility.LoadPrefabContents(oldPrefabPath);

            if (instance.transform.childCount > 0)
            {
                Transform child = instance.transform.GetChild(0);

                if (child != null)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            if (hasSubObject)
            {
                GameObject subInstance = new GameObject(textureInfo.textureName + "(sub)");
                subInstance.transform.SetParent(instance.transform);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, oldPrefabPath);
            PrefabUtility.UnloadPrefabContents(instance);
            return prefab;
        }
        
        public static void AddComponentsAssets(GameObject prefab, Vector3[] v, int[] t, TextureInfo textureInfo, string renderType, string shaderName)
        {
            //add components
            MeshFilter meshFilter = prefab.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = prefab.GetComponent<MeshRenderer>();

            if (meshFilter == null)
            {
                meshFilter = prefab.AddComponent<MeshFilter>();
            }

            if (meshRenderer == null)
            {
                meshRenderer = prefab.AddComponent<MeshRenderer>();
            }

            //create new mesh
            Mesh mesh = new Mesh()
            {
                name = renderType,
            };

            mesh.Update(v, t, textureInfo);
            meshFilter.mesh = mesh;

            //create new material
            Material material = new Material(Shader.Find(shaderName))
            {
                name = renderType,
                mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(textureInfo.textureAssetPath)
            };
            meshRenderer.sharedMaterial = material;

            //set assets as sub-asset
            AssetDatabase.AddObjectToAsset(material, prefab);
            AssetDatabase.AddObjectToAsset(mesh, prefab);
            AssetDatabase.SaveAssets();
        }

        public static void CleanUpSubAssets(GameObject prefab)
        {
            Object[] allRelatedAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(prefab));

            //clean up sub assets
            foreach (Object asset in allRelatedAssets)
            {
                if (AssetDatabase.IsSubAsset(asset) && (asset is Mesh || asset is Material))
                {
                    AssetDatabase.RemoveObjectFromAsset(asset);
                }
            }

            AssetDatabase.SaveAssets();
        }
    }

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
                    object[] args = new object[2] { 0, 0 };
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

    public static class SpriteUtil
    {
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
            Texture2D originalTexture = TextureUtil.GetOriginalTexture(texture);

            //texture.GetOriginalImageSize(out int width, out int height);
            Rect rect = new Rect(0, 0, originalTexture.width, originalTexture.height);
            Sprite sprite = Sprite.Create(originalTexture, rect, Vector2.one, 100);
            sprite.name = originalTexture.name + "(Dummy Sprite)";
            return sprite;
        }
    }
}
