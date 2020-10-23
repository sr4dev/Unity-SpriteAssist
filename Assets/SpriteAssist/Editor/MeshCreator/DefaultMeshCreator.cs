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
            Vector2[] vertices = sprite.vertices;
            ushort[] triangles = sprite.triangles;
            sprite.AddComponentsAssets(vertices, triangles, prefab, RENDER_TYPE_TRANSPARENT, data.transparentShader);
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