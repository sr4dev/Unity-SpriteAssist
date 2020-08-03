using System.Collections.Generic;
using UnityEngine;

namespace OptSprite
{
    public class SpritePreview
    {
        private List<SpritePreviewWireframe> _wireframes;
        private Rect _rect;
        private Sprite _sprite;

        public SpritePreview(SpriteConfigData.Mode mode)
        {
            _wireframes = new List<SpritePreviewWireframe>();

            ChangeMode(mode);
        }

        public void Show(Rect rect, Sprite sprite, SpriteConfigData configData, bool needPreviewUpdate)
        {
            if (needPreviewUpdate)
            {
                UpdateAndResize(rect, sprite, configData);
            }
            else if (_rect != rect)
            {
                Resize(rect);
            }

            Draw();
        }

        public void ChangeMode(SpriteConfigData.Mode mode)
        {
            Dispose();

            switch (mode)
            {
                case SpriteConfigData.Mode.TransparentMesh:
                    _wireframes.Add(new SpritePreviewWireframe(SpritePreviewWireframe.transparentColor, MeshRenderType.Transparent));
                    break;
                
                case SpriteConfigData.Mode.OpaqueMesh:
                    _wireframes.Add(new SpritePreviewWireframe(SpritePreviewWireframe.opaqueColor, MeshRenderType.Opaque));
                    break;
               
                case SpriteConfigData.Mode.Complex:
                    _wireframes.Add(new SpritePreviewWireframe(SpritePreviewWireframe.transparentColor, MeshRenderType.SeparatedTransparent));
                    _wireframes.Add(new SpritePreviewWireframe(SpritePreviewWireframe.opaqueColor, MeshRenderType.Opaque));
                    break;
            }
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

            foreach (var wireframe in _wireframes)
            {
                wireframe.UpdateAndResize(_rect, _sprite, data);
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

        private void Draw()
        {
            foreach (var wireframe in _wireframes)
            {
                wireframe.Draw(_rect, _sprite);
            }
        }

    }
}
