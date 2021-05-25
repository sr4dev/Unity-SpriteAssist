using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public class SpriteAssistEditorWindow : EditorWindow
    {
        private readonly EditorGUISplitView _verticalSplitView = new EditorGUISplitView (EditorGUISplitView.Direction.Vertical, 20);
        private SpriteInspector _spriteInspector;
        private Sprite _sprite;
        private bool _isEnabled;

        [MenuItem("Window/SpriteAssist")]
        private static void ShowWindow()
        {
            GetWindow<SpriteAssistEditorWindow>("SpriteAssist");
        }

        [MenuItem("Assets/SpriteAssist/Swap Sprite Renderers to Mesh Prefab", priority = 700)]
        private static void SwapInProject()
        {
            Object obj = Selection.activeObject;
            string s = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);

            RendererUtil.SwapAllRecursively(s);
            EditorUtility.DisplayDialog("SpriteAssist", "Done", "OK");
        }

        [MenuItem("GameObject/SpriteAssist/Swap Sprite Renderers to Mesh Prefab", priority = 21)]
        private static void SwapInHierarchy()
        {
            RendererUtil.SwapRendererSpriteToMeshInHierarchy(Selection.objects);
            EditorUtility.DisplayDialog("SpriteAssist", "Done", "OK");
        }

        private void OnGUI()
        {
            if (_spriteInspector == null)
            {
                CreateEditor();
            }
            
            if (_isEnabled)
            {
                _verticalSplitView.BeginSplitView();

                _spriteInspector.DrawHeader();
                _spriteInspector.OnInspectorGUI();

                Rect resizeHandelRect = _verticalSplitView.Split(position);
                float height = position.height - resizeHandelRect.y;

                EditorGUILayout.Space(5);
                _spriteInspector.DrawPreview(GUILayoutUtility.GetRect(height, height));
                _verticalSplitView.EndSplitView();
            }
            else
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Select a Renderer, Texture or Sprite Asset.", MessageType.Info);
            }
            
            if (_verticalSplitView.Resized)
            {  
                Repaint();
            }
        }

        private void OnEnable()
        {
            AssemblyReloadEvents.afterAssemblyReload += CreateEditor;
        }

        private void OnSelectionChange()
        {
            CreateEditor();
            Repaint();
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
