using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace OptSprite
{
    [CustomEditor(typeof(Sprite))]
    [CanEditMultipleObjects]
    public class UnitySpriteInspector : Editor
    {
        private static Type _spriteInspectorType = typeof(Editor).Assembly.GetType("UnityEditor.SpriteInspector", true);
        private static PropertyInfo _referenceTargetIndexPropertyInfo = typeof(Editor).GetProperty("referenceTargetIndex", BindingFlags.NonPublic | BindingFlags.Instance);
        
        private Editor _baseEditor;

        public override void OnInspectorGUI()
        {
            _baseEditor.OnInspectorGUI();
        }

        public override bool HasPreviewGUI()
        {
            UpdateTargetIndexForPreview();
            return _baseEditor.HasPreviewGUI();
        }

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            UpdateTargetIndexForPreview();
            _baseEditor.OnPreviewGUI(rect, background);
        }

        public override string GetInfoString()
        {
            return _baseEditor.GetInfoString();
        }

        public override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height)
        {
            return _baseEditor.RenderStaticPreview(assetPath, subAssets, width, height);
        }

        protected virtual void OnEnable()
        {
            CreateBaseEditor();
        }

        protected virtual void OnDisable()
        {
            DestroyBaseEditor();
        }

        private void CreateBaseEditor()
        {
            DestroyBaseEditor();

            _baseEditor = CreateEditor(targets, _spriteInspectorType);
        }

        private void DestroyBaseEditor()
        {
            if (_baseEditor != null)
            {
                DestroyImmediate(_baseEditor);
            }
        }

        private void UpdateTargetIndexForPreview()
        {
            int index = (int)_referenceTargetIndexPropertyInfo.GetValue(this, null);
            _referenceTargetIndexPropertyInfo.SetValue(_baseEditor, index, null);
        }
    }
}