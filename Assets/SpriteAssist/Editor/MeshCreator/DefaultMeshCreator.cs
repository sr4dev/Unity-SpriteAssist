using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpriteAssist
{
    public class DefaultMeshCreator : MeshCreatorBase
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
            Vector3[] vertices3D = Array.ConvertAll(sprite.vertices, i => new Vector3(i.x, i.y, 0));
            int[] triangles3D = Array.ConvertAll(sprite.triangles, i => (int)i);
            PrefabUtil.AddComponentsAssets(sprite, externalObject, vertices3D, triangles3D, textureInfo, RENDER_TYPE_TRANSPARENT, data.transparentShaderName, data);
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