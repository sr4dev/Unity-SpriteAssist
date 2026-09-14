using System.Collections.Generic;
using UnityEngine;

namespace SpriteAssist
{
    public class DefaultOpaqueMeshCreator : SingleMeshCreatorBase
    {
        protected override MeshRenderType MeshRenderType3D => MeshRenderType.OpaqueWithoutExtrude;

        protected override string RenderType => RENDER_TYPE_OPAQUE;

        protected override string GetShaderName(SpriteConfigData data) => data.opaqueShaderName;

        public override void OverrideGeometry(Sprite baseSprite, Sprite dummySprite, TextureInfo textureInfo, SpriteConfigData data)
        {
            DefaultMeshGeometry.Generate(baseSprite, dummySprite, textureInfo, data).ApplyToSprite(baseSprite, textureInfo);
        }

        public override List<SpritePreviewWireframe> GetMeshWireframes()
        {
            return new List<SpritePreviewWireframe>()
            {
                new SpritePreviewWireframe(SpritePreviewWireframe.opaqueColor, MeshRenderType.OpaqueWithoutExtrude)
            };
        }
    }
}
