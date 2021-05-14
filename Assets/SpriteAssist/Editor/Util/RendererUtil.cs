using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public static class RendererUtil
    {
        public static bool HasSpriteRendererAny(Object[] targets)
        {
            foreach (var target in targets)
            {
                if (PrefabUtil.TryGetMutableInstanceInHierarchy(target, out GameObject gameObject) &&
                    PrefabUtil.TryGetSpriteRendererWithSprite(gameObject, out SpriteRenderer spriteRenderer) &&
                    PrefabUtil.TryGetInternalAssetPath(spriteRenderer.sprite.texture, out string texturePath))
                {
                    SpriteImportData import = new SpriteImportData(spriteRenderer.sprite, texturePath);

                    if (import.HasMeshPrefab)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void SwapRendererSpriteToMeshInHierarchy(Object[] targets)
        {
            foreach (var target in targets)
            {
                SwapRendererSpriteToMeshInHierarchy(target);
            }
        }

        public static void SwapRendererSpriteToMeshInHierarchy(Object target, bool isRoot = false)
        {
            if (PrefabUtil.TryGetMutableInstanceInHierarchy(target, out GameObject gameObject) &&
                PrefabUtil.TryGetSpriteRendererWithSprite(gameObject, out SpriteRenderer spriteRenderer) &&
                PrefabUtil.TryGetInternalAssetPath(spriteRenderer.sprite.texture, out string texturePath))
            {
                SpriteImportData import = new SpriteImportData(spriteRenderer.sprite, texturePath);
                if (import.HasMeshPrefab)
                {
                    GameObject meshPrefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(import.MeshPrefab);
                    meshPrefabInstance.name = gameObject.name;
                    meshPrefabInstance.layer = gameObject.layer;
                    meshPrefabInstance.tag = gameObject.tag;
                    meshPrefabInstance.isStatic = gameObject.isStatic;
                    meshPrefabInstance.SetActive(gameObject.activeSelf);
                    meshPrefabInstance.transform.SetParent(gameObject.transform.parent);
                    meshPrefabInstance.transform.localPosition = gameObject.transform.localPosition;
                    meshPrefabInstance.transform.localRotation = gameObject.transform.localRotation;
                    meshPrefabInstance.transform.localScale = gameObject.transform.localScale;

                    foreach (Transform t in gameObject.transform)
                    {
                        if (PrefabUtil.IsMutablePrefab(t.gameObject))
                        {
                            t.SetParent(meshPrefabInstance.transform);
                        }
                    }

                    if (PrefabUtil.IsPrefabModeRoot(gameObject) || isRoot)
                    {
                        meshPrefabInstance.transform.SetParent(gameObject.transform);
                        Object.DestroyImmediate(spriteRenderer);
                    }
                    else
                    {
                        int index = gameObject.transform.GetSiblingIndex();
                        meshPrefabInstance.transform.SetSiblingIndex(index);
                        Object.DestroyImmediate(gameObject);
                    }

                    EditorUtility.SetDirty(meshPrefabInstance);
                }
            }
        }
        
        public static void SwapAllRecursively(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }
            
            HashSet<string> nestedPrefabPaths = new HashSet<string>();
            
            using (var loadedPrefabScope = new PrefabUtil.EditPrefabAssetScope(assetPath))
            {
                GameObject loadedPrefab = loadedPrefabScope.prefabRoot;
                Transform[] ts = loadedPrefab.GetComponentsInChildren<Transform>(true);
                
                foreach (Transform t in ts)
                {
                    if (PrefabUtility.IsPartOfAnyPrefab(t.gameObject))
                    {
                        string nestedPrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
                        nestedPrefabPaths.Add(nestedPrefabPath);
                    }
                    else
                    {
                        bool isRoot = loadedPrefab.transform == t;
                        SwapRendererSpriteToMeshInHierarchy(t.gameObject, isRoot);
                    }
                }
            }

            foreach (string s in nestedPrefabPaths)
            {
                SwapAllRecursively(s);
            }
        }
    }
}