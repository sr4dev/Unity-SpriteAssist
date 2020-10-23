using System.Collections.Generic;
using UnityEngine;

namespace SpriteAssist
{
    public class ComplexMeshCreator : MeshCreatorBase
    {
        public override void OverrideGeometry(Sprite sprite, SpriteConfigData data)
        {
            //does not supported
        }

        public override GameObject CreateExternalObject(Sprite sprite, SpriteConfigData data)
        {
            GameObject root = sprite.CreateEmptyMeshPrefab(true);
            GameObject sub = root.transform.GetChild(0).gameObject;
            sprite.GetMeshData(data, out var transparentVertices, out var transparentTriangles, MeshRenderType.SeparatedTransparent);
            sprite.GetMeshData(data, out var opaqueVertices, out var opaqueTriangles, MeshRenderType.Opaque);
            sprite.AddComponentsAssets(transparentVertices, transparentTriangles, root, RENDER_TYPE_TRANSPARENT, data.transparentShader);
            sprite.AddComponentsAssets(opaqueVertices, opaqueTriangles, sub, RENDER_TYPE_OPAQUE, data.opaqueShader);
            return root;
        }

        public override List<SpritePreviewWireframe> GetMeshWireframes()
        {
            return new List<SpritePreviewWireframe>()
            {
                new SpritePreviewWireframe(SpritePreviewWireframe.transparentColor, MeshRenderType.SeparatedTransparent),
                new SpritePreviewWireframe(SpritePreviewWireframe.opaqueColor, MeshRenderType.Opaque)
            };
        }
    }

}