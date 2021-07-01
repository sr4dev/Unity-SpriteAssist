using System;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpriteAssist
{
    public static class PrefabUtil
    {
        public class EditPrefabAssetScope : IDisposable {
        
            public readonly string assetPath;
            public readonly GameObject prefabRoot;
        
            public EditPrefabAssetScope(string assetPath) {
                this.assetPath = assetPath;
                prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
            }
        
            public void Dispose() {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
        
        public static GameObject CreateMeshPrefab(TextureInfo textureInfo, bool hasSubObject)
        {
            string prefix = SpriteAssistSettings.Settings.prefabNamePrefix;
            string suffix = SpriteAssistSettings.Settings.prefabNameSuffix;
            string objectName = $"{prefix}{textureInfo.textureName}{suffix}";

            string currentDirectory = Path.GetDirectoryName(textureInfo.textureAssetPath);
            string relativePath = SpriteAssistSettings.Settings.prefabRelativePath;
            string fileName = $"{objectName}.prefab";

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

            string path = Path.Combine(currentDirectory, fileName);
            GameObject instance = new GameObject(objectName);

            if (hasSubObject)
            {
                GameObject subInstance = new GameObject(objectName + "(sub)");
                subInstance.transform.SetParent(instance.transform);
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
                subInstance.transform.SetParent(instance.transform);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(instance, externalObjectPath, InteractionMode.AutomatedAction);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        public static void AddComponentsAssets(Sprite sprite, GameObject prefab, Vector3[] v, int[] t, TextureInfo textureInfo, string renderType, string shaderName, SpriteConfigData spriteConfigData)
        {
            prefab.layer = spriteConfigData.overrideSortingLayer ? spriteConfigData.layer : SpriteAssistSettings.Settings.defaultLayer;
            string tag = spriteConfigData.overrideTag ? spriteConfigData.tag : SpriteAssistSettings.Settings.defaultTag;

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
                meshRenderer.sortingLayerID = SpriteAssistSettings.Settings.defaultSortingLayerId;
                meshRenderer.sortingOrder = SpriteAssistSettings.Settings.defaultSortingOrder;
            }

            //create new mesh
            Mesh mesh = new Mesh()
            {
                name = renderType,
            };

            mesh.Update(v, t, textureInfo);
            meshFilter.mesh = mesh;

            //create new material
            Material material = new Material(Shader.Find(shaderName));
            material.name = renderType;
            material.mainTexture = sprite.texture;

            meshRenderer.sharedMaterial = material;

            //set assets as sub-asset
            AssetDatabase.AddObjectToAsset(material, prefab);
            AssetDatabase.AddObjectToAsset(mesh, prefab);
            //AssetDatabase.SaveAssets();
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