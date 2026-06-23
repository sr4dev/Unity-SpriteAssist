using System.Collections.Generic;
using UnityEngine;

namespace SpriteAssist
{
    public class DefaultTransparentMeshCreator : SingleMeshCreatorBase
    {
        protected override MeshRenderType MeshRenderType3D => MeshRenderType.Transparent;

        protected override string RenderType => RENDER_TYPE_TRANSPARENT;

        protected override string GetShaderName(SpriteConfigData data) => data.transparentShaderName;

        protected override Sprite GetSource3D(Sprite baseSprite, Sprite dummySprite)
        {
            return baseSprite;
        }

        public override void OverrideGeometry(Sprite baseSprite, Sprite dummySprite, TextureInfo textureInfo, SpriteConfigData data)
        {
            //use unity native mesh
        }

        public override List<SpritePreviewWireframe> GetMeshWireframes()
        {
            return new List<SpritePreviewWireframe>()
            {
                new SpritePreviewWireframe(SpritePreviewWireframe.transparentColor, MeshRenderType.Transparent)
            };
        }
    }
}
