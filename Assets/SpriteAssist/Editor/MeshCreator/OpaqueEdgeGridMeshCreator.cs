using System.Collections.Generic;
using UnityEngine;

namespace SpriteAssist
{
    public class OpaqueEdgeGridMeshCreator : MeshCreatorBase
    {
        public override void OverrideGeometry(Sprite baseSprite, Sprite dummySprite, TextureInfo textureInfo, SpriteConfigData data)
        {
            Mesh combinedMesh = GetCombinedMesh(baseSprite, dummySprite, textureInfo, data, true);
            baseSprite.OverrideGeometry(combinedMesh.vertices.ToVector2(), combinedMesh.triangles.ToUShort());
        }

        public override GameObject CreateExternalObject(Sprite sprite, TextureInfo textureInfo, SpriteConfigData data, string oldPrefabPath = null)
        {
            return PrefabUtil.CreateMeshPrefab(textureInfo, false);
        }

        public override void UpdateExternalObject(GameObject externalObject, Sprite baseSprite, Sprite dummySprite, TextureInfo textureInfo, SpriteConfigData data)
        {
            PrefabUtil.UpdateMeshPrefab(textureInfo, false, externalObject);
            Mesh combinedMesh = GetCombinedMesh(baseSprite, dummySprite, textureInfo, data, false);
            PrefabUtil.AddComponentsAssets(baseSprite, externalObject, combinedMesh.vertices, combinedMesh.triangles, textureInfo, RENDER_TYPE_OPAQUE, data.opaqueShaderName, data);
        }

        public override void UpdateMeshInMeshPrefab(GameObject externalObject, Sprite baseSprite, Sprite dummySprite, TextureInfo textureInfo, SpriteConfigData data)
        {
            Mesh combinedMesh = GetCombinedMesh(baseSprite, dummySprite, textureInfo, data, false);
            PrefabUtil.UpdateMeshFiltersMesh(externalObject, combinedMesh.vertices, combinedMesh.triangles, textureInfo, data.isCorrectNormal);
        }

        public override List<SpritePreviewWireframe> GetMeshWireframes()
        {
            return new List<SpritePreviewWireframe>()
            {
                new SpritePreviewWireframe(SpritePreviewWireframe.opaqueColor, MeshRenderType.OpaqueWithoutTightGrid),
                new SpritePreviewWireframe(SpritePreviewWireframe.opaqueColor, MeshRenderType.TightGrid)
            };
        }

        private Mesh GetCombinedMesh(Sprite baseSprite, Sprite dummySprite, TextureInfo textureInfo, SpriteConfigData data, bool applyPixelPerUnitScale)
        {
            dummySprite.GetVertexAndTriangle2D(data, out var vertices, out var triangles, MeshRenderType.OpaqueWithoutTightGrid);
            if (applyPixelPerUnitScale) vertices = MeshUtil.GetScaledVertices(vertices, textureInfo, isClamped: true);
            Mesh opaqueMesh = MeshUtil.Create(vertices.ToVector3(), triangles.ToInt(), textureInfo, false);

            dummySprite.GetVertexAndTriangle2D(data, out var verticesGrid, out var trianglesGrid, MeshRenderType.TightGrid);
            if (applyPixelPerUnitScale) verticesGrid = MeshUtil.GetScaledVertices(verticesGrid, textureInfo, isClamped: true);
            Mesh gridMesh = MeshUtil.Create(verticesGrid.ToVector3(), trianglesGrid.ToInt(), textureInfo, false);

            var combinedMesh = new Mesh();

            combinedMesh.CombineMeshes(new[]
            {
                new CombineInstance { mesh = opaqueMesh, transform = Matrix4x4.identity },
                new CombineInstance { mesh = gridMesh, transform = Matrix4x4.identity, }
            }, true);

            return combinedMesh;
        }
    }
}