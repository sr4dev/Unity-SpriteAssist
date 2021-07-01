using System.Collections.Generic;
using UnityEngine;

namespace SpriteAssist
{
    public class ComplexMeshCreator : MeshCreatorBase
    {
        public override void OverrideGeometry(Sprite baseSprite, Sprite dummySprite, TextureInfo textureInfo, SpriteConfigData data)
        {
            //does not supported
        }

        public override GameObject CreateExternalObject(Sprite sprite, TextureInfo textureInfo, SpriteConfigData data, string oldPrefabPath = null)
        {
            return PrefabUtil.CreateMeshPrefab(textureInfo, true);
        }

        public override void UpdateExternalObject(GameObject externalObject, Sprite baseSprite, Sprite dummySprite, TextureInfo textureInfo, SpriteConfigData data)
        {
            PrefabUtil.UpdateMeshPrefab(textureInfo, true, externalObject);

            GameObject root = externalObject;
            GameObject sub = root.transform.GetChild(0).gameObject;
            dummySprite.GetVertexAndTriangle3D(data, out var transparentVertices, out var transparentTriangles, MeshRenderType.SeparatedTransparent);
            dummySprite.GetVertexAndTriangle3D(data, out var opaqueVertices, out var opaqueTriangles, MeshRenderType.Opaque);
            PrefabUtil.AddComponentsAssets(baseSprite, root, transparentVertices, transparentTriangles, textureInfo, RENDER_TYPE_TRANSPARENT, data.transparentShaderName, data);
            PrefabUtil.AddComponentsAssets(baseSprite, sub, opaqueVertices, opaqueTriangles, textureInfo, RENDER_TYPE_OPAQUE, data.opaqueShaderName, data);
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