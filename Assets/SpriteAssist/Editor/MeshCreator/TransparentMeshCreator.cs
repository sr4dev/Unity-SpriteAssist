using System.Collections.Generic;

namespace SpriteAssist
{
    public class TransparentMeshCreator : SingleMeshCreatorBase
    {
        protected override MeshRenderType MeshRenderType3D => MeshRenderType.Transparent;

        protected override string RenderType => RENDER_TYPE_TRANSPARENT;

        protected override string GetShaderName(SpriteConfigData data) => data.transparentShaderName;

        public override List<SpritePreviewWireframe> GetMeshWireframes()
        {
            return new List<SpritePreviewWireframe>()
            {
                new SpritePreviewWireframe(SpritePreviewWireframe.transparentColor, MeshRenderType.Transparent)
            };
        }
    }
}
