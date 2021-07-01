using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpriteAssist
{
    public class DefaultTransparentMeshCreator : MeshCreatorBase
    {
        public override void OverrideGeometry(Sprite baseSprite, Sprite dummySprite, TextureInfo textureInfo, SpriteConfigData data)
        {
            //use unity native mesh
        }

        public override GameObject CreateExternalObject(Sprite sprite, TextureInfo textureInfo, SpriteConfigData data, string oldPrefabPath = null)
        {
            return PrefabUtil.CreateMeshPrefab(textureInfo, false);
        }

        public override void UpdateExternalObject(GameObject externalObject, Sprite baseSprite, Sprite dummySprite, TextureInfo textureInfo, SpriteConfigData data)
        {
            PrefabUtil.UpdateMeshPrefab(textureInfo, false, externalObject);

            baseSprite.GetVertexAndTriangle3D(data, out var vertices3D, out var triangles3D, MeshRenderType.Transparent);
            PrefabUtil.AddComponentsAssets(baseSprite, externalObject, vertices3D, triangles3D, textureInfo, RENDER_TYPE_TRANSPARENT, data.transparentShaderName, data);
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