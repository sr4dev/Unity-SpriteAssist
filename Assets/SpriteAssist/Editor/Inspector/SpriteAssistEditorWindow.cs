using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpriteAssist
{
    public class SpriteAssistEditorWindow : EditorWindow
    {
        private SpriteInspector _spriteInspector;
        private Sprite _sprite;
        private bool _isEnabled;
        private bool _hasSpriteRendererAny;

        [MenuItem("Window/SpriteAssist")]
        private static void ShowWindow()
        {
            GetWindow<SpriteAssistEditorWindow>("SpriteAssist");
        }

        private void OnGUI()
        {
            if (_spriteInspector == null)
            {
                CreateEditor();
            }
            
            if (_isEnabled)
            {
                if (_hasSpriteRendererAny)
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
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Select a Texture or Sprite Asset.", MessageType.Info);
            }
            
            //experimental
            if (GUILayout.Button("Swap All"))
            {
                Object obj = Selection.activeObject;
                string s = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);
                
                RendererUtil.SwapAllRecursively(s);
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
            if (_spriteInspector != null)
            {
                DestroyImmediate(_spriteInspector);
            }

            Object target = GetTargetRelatedWithTexture();

            if (TryGetSprite(target, out _sprite, out bool isTextureImporterMode))
            {
                string path = AssetDatabase.GetAssetPath(target);
                _spriteInspector = (SpriteInspector)Editor.CreateEditor(_sprite);
                _spriteInspector.SetSpriteProcessor(_sprite, path);
                _spriteInspector.SpriteProcessor.IsExtendedByEditorWindow = true;
                _spriteInspector.SpriteProcessor.IsTextureImporterMode = isTextureImporterMode;
            }

            _isEnabled = _sprite != null && _spriteInspector != null && _spriteInspector.SpriteProcessor != null;
            _hasSpriteRendererAny = RendererUtil.HasSpriteRendererAny(Selection.objects);
        }

        private static Object GetTargetRelatedWithTexture()
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

            return target;
        }

        private static bool TryGetSprite(Object target, out Sprite sprite, out bool fromTexture)
        {
            switch (target)
            {
                case Sprite value:
                    sprite = value;
                    fromTexture = false;
                    break;

                case Texture2D texture:
                    sprite = SpriteUtil.CreateDummySprite(texture);
                    fromTexture = true;
                    break;

                default:
                    sprite = null;
                    fromTexture = false;
                    break;
            }

            return sprite != null;
        }
    }
}
