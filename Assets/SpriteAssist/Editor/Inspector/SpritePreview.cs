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
        private TextureInfo _textureInfo;

        public SpritePreview(List<SpritePreviewWireframe> wireframes)
        {
            _wireframes = wireframes;
        }

        public void Show(Rect rect, Sprite sprite, TextureInfo textureInfo, SpriteConfigData configData, bool hasMultipleTargets)
        {
            _rect = rect;
            _textureInfo = textureInfo;
            _infoText = "";

            foreach (var wireframe in _wireframes)
            {
                wireframe.UpdateAndResize(_rect, sprite, textureInfo, configData);
                _infoText += wireframe.GetInfo(textureInfo) + "\n";
            }

            foreach (var wireframe in _wireframes)
            {
                wireframe.Draw(_rect, _textureInfo);
            }

            if (!hasMultipleTargets)
            {
                var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, richText = true, fontStyle = FontStyle.Bold };
                EditorGUI.DropShadowLabel(_rect, _infoText, style);
            }
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
    }
}
