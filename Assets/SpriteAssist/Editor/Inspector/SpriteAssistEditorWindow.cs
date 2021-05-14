using System.Collections.Generic;
using UnityEditor;
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
                    if (RendererUtil.HasSpriteRendererAny(Selection.objects))
                    {
                        if (GUILayout.Button("Swap SpriteRenderer to Mesh Prefab"))
                        {
                            RendererUtil.SwapRendererSpriteToMeshInHierarchy(Selection.objects);
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
            
            //experimental
            if (GUILayout.Button("Swap All"))
            {
                Object obj = Selection.activeObject;
                string s = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);
                
                RendererUtil.SwapAllRecursively(s);
            }
        }

        private void OnGUIFallback()
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Select a Texture or Sprite Asset.", MessageType.Info);
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
