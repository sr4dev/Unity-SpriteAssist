using System.Collections.Generic;
using UnityEngine;

namespace SpriteAssist
{
    public class OpaqueMeshCreator : MeshCreatorBase
    {
        public override void OverrideGeometry(Sprite sprite, SpriteConfigData data)
        {
            sprite.GetVertexAndTriangle2D(data, out var vertices, out var triangles, MeshRenderType.Opaque);
            sprite.SetSpriteScaleToVertices(vertices, 1, false, false);
            sprite.OverrideGeometry(vertices, triangles);
        }

        public override GameObject CreateExternalObject(Sprite sprite, SpriteConfigData data)
        {
            GameObject prefab = sprite.CreateEmptyMeshPrefab(false);
            sprite.GetVertexAndTriangle3D(data, out var vertices, out var triangles, MeshRenderType.Opaque);
            sprite.AddComponentsAssets(vertices, triangles, prefab, RENDER_TYPE_OPAQUE, data.opaqueShaderName);
            return prefab;
        }

        public override List<SpritePreviewWireframe> GetMeshWireframes()
        {
            return new List<SpritePreviewWireframe>()
            {
                new SpritePreviewWireframe(SpritePreviewWireframe.opaqueColor, MeshRenderType.Opaque)
            };
        }
    }

}