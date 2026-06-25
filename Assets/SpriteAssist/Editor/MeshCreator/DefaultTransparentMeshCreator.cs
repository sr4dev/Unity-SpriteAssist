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
            if (OutlineUtil.HasImporterOutline(baseSprite))
            {
                return baseSprite;
            }

            return dummySprite;
        }

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
                new SpritePreviewWireframe(SpritePreviewWireframe.transparentColor, MeshRenderType.Transparent)
            };
        }
    }
}
