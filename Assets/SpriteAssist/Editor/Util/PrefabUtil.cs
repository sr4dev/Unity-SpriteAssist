using System;
using System.Collections.Generic;
using System.IO;
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
            // 階層が既に期待通りなら prefab の再構築（Instantiate → SaveAsPrefabAsset → reimport）を省略する。
            // 大量マイグレーション時はこの再構築が 1 件あたりの主要コストになる。
            if (externalObject.transform.childCount == (hasSubObject ? 1 : 0))
            {
                return externalObject;
            }

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

        // Mesh はテクスチャのサブアセット（SpriteMeshAssets）を参照する。Material のみ prefab のサブアセットとして持つ。
        //
        // Layer / Tag / Sorting Layer / Material(shader 含む) は Mesh Prefab の「生成時の初期値」であり、
        // 既に MeshRenderer を持つ prefab（reimport・migration・Apply 時）では利用者が prefab 側で変更した値を保持する。
        // 初期値の適用は MeshRenderer を新規追加したときだけ行う。
        public static void AddComponentsAssets(Sprite sprite, GameObject prefab, Mesh mesh, string renderType, string shaderName, SpriteConfigData spriteConfigData)
        {
            //add components
            MeshFilter meshFilter = prefab.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = prefab.GetComponent<MeshRenderer>();
            bool isNewRenderer = meshRenderer == null;

            if (meshFilter == null)
            {
                meshFilter = prefab.AddComponent<MeshFilter>();
            }

            if (isNewRenderer)
            {
                meshRenderer = prefab.AddComponent<MeshRenderer>();
                ApplyInitialRendererSettings(prefab, meshRenderer, spriteConfigData);
            }

            //link imported mesh (texture sub-asset)
            if (mesh == null)
            {
                Debug.LogWarning($"[SpriteAssist] Mesh sub-asset not found for '{sprite.texture.name}'. Reimport the texture to regenerate it.");
            }

            meshFilter.sharedMesh = mesh;

            // 既存 Material があればそのまま使う（shader・プロパティ・外部 Material 参照を保持する）
            if (meshRenderer.sharedMaterial == null)
            {
                //create new material
                Material material = new Material(Shader.Find(shaderName));
                material.name = renderType;
                material.SetMainTexture(sprite.texture);

                meshRenderer.sharedMaterial = material;

                //set material as sub-asset
                AssetDatabase.AddObjectToAsset(material, prefab);
            }

            EditorUtility.SetDirty(prefab);
        }

        // 生成時のみ適用する初期値（Layer / Tag / Sorting Layer / Sorting Order）
        private static void ApplyInitialRendererSettings(GameObject prefab, MeshRenderer meshRenderer, SpriteConfigData spriteConfigData)
        {
            SpriteAssistSettings settings = SpriteAssistSettings.instance;

            prefab.layer = spriteConfigData.overrideLayer ? spriteConfigData.layer : settings.defaultLayer;

            string tag = spriteConfigData.overrideTag ? spriteConfigData.tag : settings.defaultTag;
            if (string.IsNullOrEmpty(tag) || Array.IndexOf(UnityEditorInternal.InternalEditorUtility.tags, tag) < 0)
            {
                tag = SpriteAssistSettings.DEFAULT_TAG;
            }

            prefab.tag = tag;

            if (spriteConfigData.overrideSortingLayer)
            {
                meshRenderer.sortingLayerID = spriteConfigData.sortingLayerId;
                meshRenderer.sortingOrder = spriteConfigData.sortingOrder;
            }
            else
            {
                meshRenderer.sortingLayerID = settings.defaultSortingLayerId;
                meshRenderer.sortingOrder = settings.defaultSortingOrder;
            }
        }

        // 旧構造で prefab に埋め込まれた Mesh と、どの Renderer からも参照されていない Material サブアセットを除去する。
        // Renderer が参照している Material は利用者の変更を保持するため残す。
        public static void CleanUpSubAssets(GameObject prefab)
        {
            var referencedMaterials = new HashSet<Material>();
            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        referencedMaterials.Add(material);
                    }
                }
            }

            Object[] allRelatedAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(prefab));

            //clean up sub assets
            foreach (Object asset in allRelatedAssets)
            {
                if (!AssetDatabase.IsSubAsset(asset)) continue;

                bool isEmbeddedMesh = asset is Mesh;
                bool isOrphanMaterial = asset is Material material && !referencedMaterials.Contains(material);

                if (isEmbeddedMesh || isOrphanMaterial)
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
