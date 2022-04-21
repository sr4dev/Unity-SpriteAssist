using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public class SpritePreview : IDisposable
    {
        public Rect Rect { get; private set; }

        private string _infoText;
        private TextureInfo _textureInfo;
        private List<SpritePreviewWireframe> _wireframes;

        public SpritePreview(List<SpritePreviewWireframe> wireframes)
        {
            _wireframes = wireframes;
        }

        public void Update(Rect rect, Sprite baseSprite, Sprite dummySprite, TextureInfo textureInfo, SpriteConfigData configData)
        {
            if (Application.isPlaying)
            {
                return;
            }

            Rect = rect;

            _textureInfo = textureInfo;
            _infoText = "";

            foreach (var wireframe in _wireframes)
            {
                wireframe.UpdateAndResize(this.Rect, baseSprite, dummySprite, _textureInfo, configData);

                var info = wireframe.GetInfo(textureInfo);
                
                if (string.IsNullOrEmpty(info) == false)
                {
                    _infoText += info + "\n";
                }
            }
        }

        public void Show(bool hasMultipleTargets)
        {
            if (Application.isPlaying)
            {
                return;
            }

            foreach (var wireframe in _wireframes)
            {
                wireframe.Draw(Rect, _textureInfo);
            }

            if (!hasMultipleTargets)
            {
                var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, richText = true, fontStyle = FontStyle.Bold };
                EditorGUI.DropShadowLabel(Rect, _infoText, style);
            }
        }

        public void SetWireframes(List<SpritePreviewWireframe> wireframes)
        {
            if (Application.isPlaying)
            {
                return;
            }

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
