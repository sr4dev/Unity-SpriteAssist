﻿using System.IO;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;

namespace SpriteAssist
{
    public static class PrefabUtil
    {
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

        public static bool TryGetSpriteFromInstance(GameObject gameObject, out Sprite sprite)
        {
            SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                sprite = spriteRenderer.sprite;
                return true;
            }

            sprite = null;
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
    }
}
