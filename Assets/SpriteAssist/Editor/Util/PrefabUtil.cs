using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.SceneManagement;

#else
using UnityEditor.Experimental.SceneManagement;
#endif

namespace SpriteAssist
{
    public static class PrefabUtil
    {
        public class EditPrefabAssetScope : IDisposable
        {
            public readonly string assetPath;
            public readonly GameObject prefabRoot;

            public EditPrefabAssetScope(string assetPath)
            {
                this.assetPath = assetPath;
                prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
            }

            public void Dispose()
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        public static bool IsAssetImportWorkerProcess()
        {
            return AssetDatabase.IsAssetImportWorkerProcess();
        }

        public static GameObject CreateMeshPrefab(TextureInfo textureInfo, bool hasSubObject)
        {
            string prefix = SpriteAssistSettings.instance.prefabNamePrefix;
            string suffix = SpriteAssistSettings.instance.prefabNameSuffix;
            string textureFileName = Path.GetFileNameWithoutExtension(textureInfo.textureAssetPath);
            string objectName = $"{prefix}{textureFileName}{suffix}";
            string currentDirectory = Path.GetDirectoryName(textureInfo.textureAssetPath);
            string relativePath = SpriteAssistSettings.instance.prefabRelativePath;

            string path;
            int count = 0;
            do
            {
                count++;

                string countText = count > 1 ? $"_{count}" : string.Empty;
                path = Path.Combine(currentDirectory, $"{textureFileName}{countText}.prefab");
            } while (File.Exists(path));

            if (!string.IsNullOrEmpty(relativePath))
            {
                int length = Path.GetDirectoryName(Application.dataPath).Length;
                currentDirectory = Path.GetFullPath(Path.Combine(currentDirectory, relativePath));
                currentDirectory = currentDirectory.Substring(length + 1);

                if (!Directory.Exists(Path.GetDirectoryName(currentDirectory)))
                {
                    //create all directories and subdirectories
                    Directory.CreateDirectory(currentDirectory);
                }
            }

            GameObject instance = new GameObject(objectName);

            if (hasSubObject)
            {
                GameObject subInstance = new GameObject(objectName + "(sub)");
                subInstance.transform.SetParent(instance.transform, false);
                subInstance.transform.localPosition = default;
                subInstance.transform.localRotation = default;
                subInstance.transform.localScale = Vector3.one;
            }

            PrefabUtility.SaveAsPrefabAssetAndConnect(instance, path, InteractionMode.AutomatedAction);
            Object.DestroyImmediate(instance);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        public static GameObject UpdateMeshPrefab(TextureInfo textureInfo, bool hasSubObject, GameObject externalObject)
        {
            var externalObjectPath = AssetDatabase.GetAssetPath(externalObject);
            GameObject instance = PrefabUtility.InstantiatePrefab(externalObject) as GameObject;
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);

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
                GameObject subInstance = new GameObject(instance.transform.name + "(sub)");
                subInstance.transform.SetParent(instance.transform, false);
                subInstance.transform.localPosition = default;
                subInstance.transform.localRotation = default;
                subInstance.transform.localScale = Vector3.one;
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(instance, externalObjectPath, InteractionMode.AutomatedAction);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        public static void AddComponentsAssets(Sprite sprite, GameObject prefab, Vector3[] v, int[] t, TextureInfo textureInfo, string renderType, string shaderName, SpriteConfigData spriteConfigData)
        {
            prefab.layer = spriteConfigData.overrideSortingLayer ? spriteConfigData.layer : SpriteAssistSettings.instance.defaultLayer;
            string tag = spriteConfigData.overrideTag ? spriteConfigData.tag : SpriteAssistSettings.instance.defaultTag;

            if (string.IsNullOrEmpty(tag))
            {
                prefab.tag = SpriteAssistSettings.DEFAULT_TAG;
            }

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

            if (spriteConfigData.overrideSortingLayer)
            {
                meshRenderer.sortingLayerID = spriteConfigData.sortingLayerId;
                meshRenderer.sortingOrder = spriteConfigData.sortingOrder;
            }
            else
            {
                meshRenderer.sortingLayerID = SpriteAssistSettings.instance.defaultSortingLayerId;
                meshRenderer.sortingOrder = SpriteAssistSettings.instance.defaultSortingOrder;
            }

            //create new mesh
            Mesh mesh = MeshUtil.Update(null, v, t, textureInfo, spriteConfigData.isCorrectNormal, spriteConfigData.isWeldVertices);
            mesh.name = renderType;
            meshFilter.mesh = mesh;

            //create new material
            Material material = new Material(Shader.Find(shaderName));
            material.name = renderType;
            material.SetMainTexture(sprite.texture);

            meshRenderer.sharedMaterial = material;

            //set assets as sub-asset
            AssetDatabase.AddObjectToAsset(material, prefab);
            AssetDatabase.AddObjectToAsset(mesh, prefab);
            //AssetDatabase.SaveAssets();
        }

        public static bool UpdateMeshFiltersMesh(GameObject prefab, Vector3[] v, int[] t, TextureInfo textureInfo, bool splitVertices, bool weldVertices = false)
        {
            MeshFilter meshFilter = prefab.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null)
            {
                meshFilter.sharedMesh = MeshUtil.Update(null, v, t, textureInfo, splitVertices, weldVertices);
                EditorUtility.SetDirty(meshFilter.sharedMesh);
                return true;
            }

            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Vector2[] uv = mesh.uv;
            Vector3[] normals = mesh.normals;
            Color[] colors = mesh.colors;
            Vector4[] tangents = mesh.tangents;
            Bounds bounds = mesh.bounds;
            bool wasDirty = EditorUtility.IsDirty(mesh);

            // 既存 Mesh と同一ジオメトリなら dirty にせず false を返す。
            // clean import 時に不要な prefab 保存・強制 reimport を発生させないため。
            MeshUtil.Update(mesh, v, t, textureInfo, splitVertices, weldVertices);

            bool changed = !vertices.SequenceEqual(mesh.vertices) ||
                           !triangles.SequenceEqual(mesh.triangles) ||
                           !uv.SequenceEqual(mesh.uv) ||
                           !normals.SequenceEqual(mesh.normals) ||
                           !colors.SequenceEqual(mesh.colors) ||
                           !tangents.SequenceEqual(mesh.tangents) ||
                           bounds != mesh.bounds;
            if (changed)
            {
                EditorUtility.SetDirty(mesh);
            }
            else if (!wasDirty)
            {
                EditorUtility.ClearDirty(mesh);
            }

            return changed;
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

            //AssetDatabase.SaveAssets();
        }

        public static bool IsMutablePrefab(GameObject gameObject)
        {
            return !(PrefabUtility.IsAnyPrefabInstanceRoot(gameObject) ^ PrefabUtility.IsPartOfPrefabInstance(gameObject));
        }

        public static bool TryGetMutableInstanceInHierarchy(Object target, out GameObject gameObject)
        {
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(target)) && target is GameObject go && IsMutablePrefab(go))
            {
                gameObject = go;
                return true;
            }

            gameObject = null;
            return false;
        }

        public static bool TryGetSpriteRendererWithSprite(GameObject gameObject, out SpriteRenderer spriteRenderer)
        {
            spriteRenderer = gameObject.GetComponent<SpriteRenderer>();

            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                return true;
            }

            return false;
        }

        public static bool TryGetInternalAssetPath(Object obj, out string path)
        {
            path = AssetDatabase.GetAssetPath(obj);
            return !string.IsNullOrEmpty(path) && path.StartsWith("Assets");
        }

        public static bool IsPrefabModeRoot(GameObject test)
        {
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            return prefabStage != null && prefabStage.prefabContentsRoot == test;
        }

        public static bool TryRename(string spriteAssetPath, GameObject meshPrefab)
        {
            var currentMeshPrefabPath = AssetDatabase.GetAssetPath(meshPrefab);

            var spriteAssetName = Path.GetFileNameWithoutExtension(spriteAssetPath);
            var meshPrefabName = Path.GetFileNameWithoutExtension(currentMeshPrefabPath);

            if (spriteAssetName != meshPrefabName)
            {
                AssetDatabase.RenameAsset(currentMeshPrefabPath, spriteAssetName);
                Debug.Log($"Mesh Prefab Renamed: {currentMeshPrefabPath}, {meshPrefabName} -> {spriteAssetName}");
                return true;
            }

            return false;
        }
    }
}
