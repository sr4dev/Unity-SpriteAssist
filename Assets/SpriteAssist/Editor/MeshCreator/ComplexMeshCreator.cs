using System.Collections.Generic;
using UnityEngine;

namespace SpriteAssist
{
    public class ComplexMeshCreator : MeshCreatorBase
    {
        public override void OverrideGeometry(Sprite sprite, TextureInfo textureInfo, SpriteConfigData data)
        {
            //does not supported
        }

        public override GameObject CreateExternalObject(Sprite sprite, TextureInfo textureInfo, SpriteConfigData data, string oldPrefabPath = null)
        {
            GameObject root = PrefabUtil.UpdateMeshPrefab(textureInfo, true, oldPrefabPath);
            GameObject sub = root.transform.GetChild(0).gameObject;
            sprite.GetVertexAndTriangle3D(data, out var transparentVertices, out var transparentTriangles, MeshRenderType.SeparatedTransparent);
            sprite.GetVertexAndTriangle3D(data, out var opaqueVertices, out var opaqueTriangles, MeshRenderType.Opaque);
            PrefabUtil.AddComponentsAssets(root, transparentVertices, transparentTriangles, textureInfo, RENDER_TYPE_TRANSPARENT, data.transparentShaderName, data);
            PrefabUtil.AddComponentsAssets(sub, opaqueVertices, opaqueTriangles, textureInfo, RENDER_TYPE_OPAQUE, data.opaqueShaderName, data);
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