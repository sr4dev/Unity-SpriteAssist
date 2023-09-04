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
            if (_baseEditor != null) 
                _baseEditor.OnInspectorGUI();
        }

        public override bool HasPreviewGUI()
        {
            UpdateTargetIndexForPreview();

            if (_baseEditor != null)
                return _baseEditor.HasPreviewGUI();
            else
                return false;
        }

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            UpdateTargetIndexForPreview();

            if (_baseEditor != null)
                _baseEditor.OnPreviewGUI(rect, background);
        }

        public override string GetInfoString()
        {
            if (_baseEditor != null)
                return _baseEditor.GetInfoString();
            else
                return null;
        }
        public override bool RequiresConstantRepaint()
        {
            if (_baseEditor != null)
                return _baseEditor.RequiresConstantRepaint();
            else
                return false;
        }

        public override GUIContent GetPreviewTitle()
        {
            if (_baseEditor != null)
                return _baseEditor.GetPreviewTitle();
            else
                return null;
        }

        public override void OnPreviewSettings()
        {
            if (_baseEditor != null)
                _baseEditor.OnPreviewSettings();
        }

        public override void ReloadPreviewInstances()
        {
            if (_baseEditor != null)
                _baseEditor.ReloadPreviewInstances();
        }

        public override bool UseDefaultMargins()
        {
            if (_baseEditor != null)
                return _baseEditor.UseDefaultMargins();
            else
                return false;
        }


        protected override void OnHeaderGUI()
        {
            UpdateTargetIndexForPreview();

            if (_baseEditor != null)
                _onHeaderGUIMethodInfo.Invoke(_baseEditor, null);
        }

        public override string ToString()
        {
            if (_baseEditor != null)
                return _baseEditor.ToString();
            else
                return null;
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            if (_baseEditor != null)
                return _baseEditor.RenderStaticPreview(assetPath, subAssets, width, height);
            else
                return null;
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
            if (_baseEditor != null)
            {
                int index = (int)_referenceTargetIndexPropertyInfo.GetValue(this, null);
                _referenceTargetIndexPropertyInfo.SetValue(_baseEditor, index, null);
            }
        }
    }
}