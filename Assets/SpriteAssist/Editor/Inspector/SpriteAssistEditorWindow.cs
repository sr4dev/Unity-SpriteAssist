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
            if (Selection.activeObject == null || _sprite == null)
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

            Object target = Selection.activeObject;

            //is in hierarchy
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(target)) && target is GameObject gameObject)
            {
                var spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    string texturePath = AssetDatabase.GetAssetPath(spriteRenderer.sprite.texture);
                    SpriteImportData import = new SpriteImportData(spriteRenderer.sprite, texturePath);

                    if (import.HasMeshPrefab && GUILayout.Button("Swap SpriteRenderer to Mesh Prefab"))
                    {
                        GameObject meshPrefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(import.MeshPrefab);
                        meshPrefabInstance.transform.SetParent(gameObject.transform.parent);
                        meshPrefabInstance.transform.localPosition = gameObject.transform.localPosition;
                        meshPrefabInstance.transform.localRotation = gameObject.transform.localRotation;
                        meshPrefabInstance.transform.localScale = gameObject.transform.localScale;

                        DestroyImmediate(gameObject);
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