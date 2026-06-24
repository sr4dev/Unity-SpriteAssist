using System.Collections.Generic;

namespace SpriteAssist
{
    public class PixelMeshCreator : SingleMeshCreatorBase
    {
        protected override MeshRenderType MeshRenderType3D => MeshRenderType.Pixel;

        protected override string RenderType => RENDER_TYPE_OPAQUE;

        protected override string GetShaderName(SpriteConfigData data) => data.opaqueShaderName;

        public override List<SpritePreviewWireframe> GetMeshWireframes()
        {
            return new List<SpritePreviewWireframe>()
            {
                new SpritePreviewWireframe(SpritePreviewWireframe.opaqueColor, MeshRenderType.Pixel)
            };
        }
    }
}
