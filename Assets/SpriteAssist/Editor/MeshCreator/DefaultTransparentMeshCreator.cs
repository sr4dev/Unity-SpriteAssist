using System.Collections.Generic;
using UnityEngine;

namespace SpriteAssist
{
    public class DefaultTransparentMeshCreator : SingleMeshCreatorBase
    {
        protected override MeshRenderType MeshRenderType3D => MeshRenderType.Transparent;

        protected override string RenderType => RENDER_TYPE_TRANSPARENT;

        protected override string GetShaderName(SpriteConfigData data) => data.transparentShaderName;

        public override void OverrideGeometry(Sprite baseSprite, Sprite dummySprite, TextureInfo textureInfo, SpriteConfigData data)
        {
            DefaultMeshGeometry.Generate(baseSprite, dummySprite, textureInfo, data).ApplyToSprite(baseSprite, textureInfo);
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
