using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpriteAssist
{
    public class SpriteAssistEditorWindow : EditorWindow
    {
        private SpriteInspector _spriteInspector;

        [MenuItem("Window/SpriteAssist")]
        private static void ShowWindow()
        {
            GetWindow<SpriteAssistEditorWindow>("SpriteAssist");
        }

        private void OnGUI()
        {
            Object target = Selection.activeObject;
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
                OnGUIFallback();
                return;
            }

            if (_spriteInspector == null)
            {
                CreateEditor();
            }

            if (_spriteInspector == null || _spriteInspector.SpriteProcessor == null)
            {
                OnGUIFallback();
                return;
            }

            _spriteInspector.SpriteProcessor.IsExtendedByEditorWindow = true;
            _spriteInspector.SpriteProcessor.IsTextureImporterMode = isTextureImporterMode;

            _spriteInspector.DrawHeader();
            _spriteInspector.OnInspectorGUI();
            GUILayout.FlexibleSpace();
            _spriteInspector.DrawPreview(GUILayoutUtility.GetRect(position.width, 300));
            GUILayout.Space(30);
        }

        private void OnGUIFallback()
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Select a Texture or Sprite Asset.", MessageType.Info);
        }

        
        private void OnSelectionChange()
        {
            Repaint();
            CreateEditor();
        }

        private void CreateEditor()
        {
            Object target = Selection.activeObject;

            Sprite sprite;

            switch (target)
            {
                case Sprite value:
                    sprite = value;
                    break;

                case Texture2D texture:
                    sprite = SpriteUtil.CreateDummySprite(texture);
                    break;

                default:
                    sprite = null;
                    break;
            }

            if (sprite == null)
            {
                return;
            }

            if (_spriteInspector != null)
            {
                DestroyImmediate(_spriteInspector);
            }

            string path = AssetDatabase.GetAssetPath(target);
            _spriteInspector = (SpriteInspector)Editor.CreateEditor(sprite);
            _spriteInspector.SetSpriteProcessor(sprite, path);
        }
    }
}