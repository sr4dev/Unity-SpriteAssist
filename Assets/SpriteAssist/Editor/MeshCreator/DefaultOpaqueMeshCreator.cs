using System.Collections.Generic;
using UnityEngine;

namespace SpriteAssist
{
    public class DefaultOpaqueMeshCreator : SingleMeshCreatorBase
    {
        protected override MeshRenderType MeshRenderType3D => MeshRenderType.OpaqueWithoutExtrude;

        protected override string RenderType => RENDER_TYPE_OPAQUE;

        protected override string GetShaderName(SpriteConfigData data) => data.opaqueShaderName;

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
                new SpritePreviewWireframe(SpritePreviewWireframe.opaqueColor, MeshRenderType.OpaqueWithoutExtrude)
            };
        }
    }
}
