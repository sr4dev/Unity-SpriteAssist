using System.Collections.Generic;
using UnityEngine;

namespace SpriteAssist
{
    public class DefaultOpaqueMeshCreator : MeshCreatorBase
    {
        public override void OverrideGeometry(Sprite sprite, TextureInfo textureInfo, SpriteConfigData data)
        {
            //use unity native mesh
        }

        public override GameObject CreateExternalObject(Sprite sprite, TextureInfo textureInfo, SpriteConfigData data, string oldPrefabPath = null)
        {
            return PrefabUtil.UpdateMeshPrefab(textureInfo, false, oldPrefabPath);
        }

        public override void UpdateExternalObject(GameObject externalObject, Sprite sprite, TextureInfo textureInfo, SpriteConfigData data)
        {
            sprite.GetVertexAndTriangle3D(data, out var vertices3D, out var triangles3D, MeshRenderType.OpaqueWithoutExtrude);
            PrefabUtil.AddComponentsAssets(sprite, externalObject, vertices3D, triangles3D, textureInfo, RENDER_TYPE_OPAQUE, data.opaqueShaderName, data);
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