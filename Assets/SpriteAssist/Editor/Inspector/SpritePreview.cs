using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public class SpritePreview : IDisposable
    {
        private List<SpritePreviewWireframe> _wireframes;
        private Rect _rect;
        private string _infoText;
        private Sprite _sprite;

        public SpritePreview(List<SpritePreviewWireframe> wireframes)
        {
            _wireframes = wireframes;
        }

        public void Show(Rect rect, Sprite sprite, SpriteConfigData configData, bool hasMultipleTargets)
        {
            UpdateAndResize(rect, sprite, configData);
            Draw(hasMultipleTargets);
        }

        public void SetWireframes(List<SpritePreviewWireframe> wireframes)
        {
            Dispose();

            _wireframes = wireframes;
        }

        public void Dispose()
        {
            foreach (var wireframe in _wireframes)
            {
                wireframe.Dispose();
            }

            _wireframes.Clear();
        }

        private void UpdateAndResize(Rect rect, Sprite sprite, SpriteConfigData data)
        {
            _rect = rect;
            _sprite = sprite;
            _infoText = "";

            foreach (var wireframe in _wireframes)
            {
                wireframe.UpdateAndResize(_rect, _sprite, data);
                _infoText += wireframe.GetInfo(_sprite) + "\n";
            }
        }

        private void Resize(Rect rect)
        {
            _rect = rect;

            foreach (var wireframe in _wireframes)
            {
                wireframe.Resize(_rect, _sprite);
            }
        }

        private void Draw(bool hasMultipleTargets)
        {
            foreach (var wireframe in _wireframes)
            {
                wireframe.Draw(_rect, _sprite);
            }

            if (!hasMultipleTargets)
            {
                var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, richText = true, fontStyle = FontStyle.Bold };
                EditorGUI.DropShadowLabel(_rect, _infoText, style);
            }
        }
    }
}
