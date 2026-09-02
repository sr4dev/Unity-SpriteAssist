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
            string assetPath = textureInfo.textureAssetPath;
            var sourceSprite = OutlineUtil.HasImporterOutline(baseSprite, assetPath) ? baseSprite : dummySprite;
            sourceSprite.GetVertexAndTriangle2D(data, out var vertices, out var triangles, MeshRenderType3D, assetPath);
            vertices = MeshUtil.GetScaledVertices(vertices, textureInfo, isClamped: true);
            baseSprite.OverrideGeometry(vertices, triangles);
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
