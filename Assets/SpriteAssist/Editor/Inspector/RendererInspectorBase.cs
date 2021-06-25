using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public abstract class RendererInspectorBase<T> : Editor where T : Renderer
    {
        private SpriteInspector _spriteInspector;

        private Editor _editor;

        private static bool _isOpen;

        protected Sprite sprite;

        protected T Renderer { get; private set; }

        protected virtual void OnEnable()
        {
            var type = typeof(EditorApplication).Assembly.GetType($"UnityEditor.{typeof(T).Name}Editor");
            _editor = CreateEditor(targets, type);

            Renderer = target as T;
        }

        protected virtual void OnDisable()
        {
            DestroyImmediate(_editor);
            DestroyImmediate(_spriteInspector);
        }

        public override void OnInspectorGUI()
        {
            _editor.OnInspectorGUI();
            
            CreateSpriteAssistEditor();

            if (_spriteInspector == null)
                return;

            GUIStyle style = EditorStyles.foldout;
            FontStyle previousStyle = style.fontStyle;
            style.fontStyle = FontStyle.Bold;
            _isOpen = EditorGUILayout.Foldout(_isOpen, "SpriteAssist", style);
            style.fontStyle = previousStyle;

            if (_isOpen)
            {
                _spriteInspector.OnInspectorGUI();

                var oldColor = GUI.color;
                GUI.color = Color.black;

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    GUI.color = oldColor;

                    var width = EditorGUIUtility.currentViewWidth;
                    _spriteInspector.DrawPreview(GUILayoutUtility.GetRect(width, 300));
                }

                if (Renderer is SpriteRenderer && GUILayout.Button("Swap to Mesh Prefab"))
                {
                    RendererUtil.SwapRendererSpriteToMeshInHierarchy(targets);
                    EditorUtility.DisplayDialog("SpriteAssist", "Done", "OK");
                }

            }
        }

        private void CreateSpriteAssistEditor()
        {
            var rendererSprite = GetRendererSprite();

            if (_spriteInspector == null || sprite != rendererSprite)
            {
                sprite = rendererSprite;

                if (_spriteInspector != null)
                {
                    DestroyImmediate(_spriteInspector);
                }

                if (sprite != null)
                {
                    string path = AssetDatabase.GetAssetPath(target);
                    _spriteInspector = (SpriteInspector)Editor.CreateEditor(sprite);
                    _spriteInspector.SetSpriteProcessor(sprite, path);
                    _spriteInspector.disableBaseGUI = true;
                }
            }
        }
        
        public abstract Sprite GetRendererSprite();
    }
}