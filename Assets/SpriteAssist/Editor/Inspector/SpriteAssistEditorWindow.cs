using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpriteAssist
{
    public class SpriteAssistEditorWindow : EditorWindow
    {
        private SpriteInspector _spriteInspector;
        private Sprite _sprite;

        [MenuItem("Window/SpriteAssist")]
        private static void ShowWindow()
        {
            GetWindow<SpriteAssistEditorWindow>("SpriteAssist");
        }

        private void OnGUI()
        {
            bool fallback = false;

            if (Selection.activeObject != null && _sprite != null)
            {
                if (_spriteInspector == null)
                {
                    CreateEditor();
                }

                if (_spriteInspector != null && _spriteInspector.SpriteProcessor != null)
                {
                    if (HasSpriteRendererAny(Selection.objects))
                    {
                        if (GUILayout.Button("Swap SpriteRenderer to Mesh Prefab"))
                        {
                            SwapSpriteRenderer(Selection.objects);
                        }

                        EditorGUILayout.HelpBox("Mesh Prefab found. You can swap this SpriteRenderer to Mesh Prefab.", MessageType.Info);
                    }

                    _spriteInspector.DrawHeader();
                    _spriteInspector.OnInspectorGUI();
                    GUILayout.FlexibleSpace();
                    GUILayout.Space(30);
                    _spriteInspector.DrawPreview(GUILayoutUtility.GetRect(position.width, position.width / 2));
                    GUILayout.Space(30);
                }
                else
                {
                    fallback = true;
                }
            }
            else
            {
                fallback = true;
            }

            if (fallback)
            {
                OnGUIFallback();
            }
        }

        private void OnGUIFallback()
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Select a Texture or Sprite Asset.", MessageType.Info);
        }

        private static bool HasSpriteRendererAny(Object[] targets)
        {
            foreach (var target in targets)
            {
                if (PrefabUtil.TryGetMutableInstanceInHierarchy(target, out GameObject gameObject) &&
                    PrefabUtil.TryGetSpriteFromInstance(gameObject, out Sprite sprite) &&
                    PrefabUtil.TryGetInternalAssetPath(sprite.texture, out string texturePath))
                {
                    SpriteImportData import = new SpriteImportData(sprite, texturePath);

                    if (import.HasMeshPrefab)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void SwapSpriteRenderer(Object[] targets)
        {
            foreach (var target in targets)
            {
                if (PrefabUtil.TryGetMutableInstanceInHierarchy(target, out GameObject gameObject) &&
                    PrefabUtil.TryGetSpriteFromInstance(gameObject, out Sprite sprite) &&
                    PrefabUtil.TryGetInternalAssetPath(sprite.texture, out string texturePath))
                {
                    SpriteImportData import = new SpriteImportData(sprite, texturePath);

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

                        int index = gameObject.transform.GetSiblingIndex();
                        meshPrefabInstance.transform.SetSiblingIndex(index);

                        if (!PrefabUtil.IsPrefabModeRoot(gameObject))
                        {
                            DestroyImmediate(gameObject);
                        }

                        EditorUtility.SetDirty(meshPrefabInstance);
                    }
                }
            }
        }

        private void OnEnable()
        {
            AssemblyReloadEvents.afterAssemblyReload += CreateEditor;
        }

        private void OnSelectionChange()
        {
            Repaint();
            CreateEditor();
        }

        private void CreateEditor()
        {
            Object target = Selection.activeObject;

            if (target is GameObject gameObject)
            {
                if (gameObject.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
                {
                    if (spriteRenderer.sprite != null)
                    {
                        target = spriteRenderer.sprite.texture;
                    }
                }
                else if (gameObject.TryGetComponent<MeshRenderer>(out var meshRenderer))
                {
                    if (meshRenderer.sharedMaterial != null)
                    {
                        target = meshRenderer.sharedMaterial.mainTexture;
                    }
                }
            }

            Sprite sprite;
            bool isTextureImporterMode;

            switch (target)
            {
                case Sprite value:
                    sprite = value;
                    isTextureImporterMode = false;
                    break;

                case Texture2D texture:
                    sprite = SpriteUtil.CreateDummySprite(texture);
                    isTextureImporterMode = true;
                    break;

                default:
                    sprite = null;
                    isTextureImporterMode = false;
                    break;
            }

            if (sprite == null)
            {
                _sprite = null;
                return;
            }

            _sprite = sprite;

            if (_spriteInspector != null)
            {
                DestroyImmediate(_spriteInspector);
            }

            string path = AssetDatabase.GetAssetPath(target);
            _spriteInspector = (SpriteInspector)Editor.CreateEditor(sprite);
            _spriteInspector.SetSpriteProcessor(sprite, path);
            _spriteInspector.SpriteProcessor.IsExtendedByEditorWindow = true;
            _spriteInspector.SpriteProcessor.IsTextureImporterMode = isTextureImporterMode;
        }
    }
}
