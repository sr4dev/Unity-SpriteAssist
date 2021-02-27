using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpriteAssist
{
    public class DefaultMeshCreator : MeshCreatorBase
    {
        public override void OverrideGeometry(Sprite sprite, SpriteConfigData data)
        {
            //use unity native mesh
        }

        public override GameObject CreateExternalObject(Sprite sprite, SpriteConfigData data)
        {
            GameObject prefab = sprite.CreateEmptyMeshPrefab(false);
            Vector3[] vertices3D = Array.ConvertAll(sprite.vertices, i => new Vector3(i.x, i.y, 0));
            int[] triangles3D = Array.ConvertAll(sprite.triangles, i => (int)i);
            sprite.AddComponentsAssets(vertices3D, triangles3D, prefab, RENDER_TYPE_TRANSPARENT, data.transparentShader);
            return prefab;
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