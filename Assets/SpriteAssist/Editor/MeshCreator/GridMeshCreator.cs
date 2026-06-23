using System.Collections.Generic;

namespace SpriteAssist
{
    public class GridMeshCreator : SingleMeshCreatorBase
    {
        protected override MeshRenderType MeshRenderType3D => MeshRenderType.Grid;

        protected override string RenderType => RENDER_TYPE_TRANSPARENT;

        protected override string GetShaderName(SpriteConfigData data) => data.transparentShaderName;

        public override List<SpritePreviewWireframe> GetMeshWireframes()
        {
            return new List<SpritePreviewWireframe>()
            {
                new SpritePreviewWireframe(SpritePreviewWireframe.transparentColor, MeshRenderType.Grid)
            };
        }
    }
}
