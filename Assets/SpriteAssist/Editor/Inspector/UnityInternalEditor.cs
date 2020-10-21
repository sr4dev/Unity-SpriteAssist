using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpriteAssist
{
    public class UnityInternalEditor<T> : Editor
    {
        private static readonly PropertyInfo _referenceTargetIndexPropertyInfo = typeof(Editor).GetProperty("referenceTargetIndex", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo _onHeaderGUIMethodInfo = typeof(Editor).GetMethod("OnHeaderGUI", BindingFlags.NonPublic | BindingFlags.Instance);
        
        protected readonly Type _inspectorType = typeof(Editor).Assembly.GetType($"UnityEditor.{typeof(T).Name}Inspector", true);
        protected Editor _baseEditor;

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
        public override bool RequiresConstantRepaint()
        {
            return _baseEditor.RequiresConstantRepaint();
        }

        public override GUIContent GetPreviewTitle()
        {
            return _baseEditor.GetPreviewTitle();
        }

        public override void OnPreviewSettings()
        {
            _baseEditor.OnPreviewSettings();
        }

        public override void ReloadPreviewInstances()
        {
            _baseEditor.ReloadPreviewInstances();
        }

        public override bool UseDefaultMargins()
        {
            return _baseEditor.UseDefaultMargins();
        }


        protected override void OnHeaderGUI()
        {
            UpdateTargetIndexForPreview();
            
            _onHeaderGUIMethodInfo.Invoke(_baseEditor, null);
        }

        public override string ToString()
        {
            return _baseEditor.ToString();
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
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

            _baseEditor = CreateEditor(targets, _inspectorType);
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