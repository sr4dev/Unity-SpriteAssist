using System.Collections.Generic;
using UnityEngine;

namespace SpriteAssist
{
    public class OpaqueEdgeGridMeshCreator : MeshCreatorBase
    {
        public override void OverrideGeometry(Sprite baseSprite, Sprite dummySprite, TextureInfo textureInfo, SpriteConfigData data)
        {
            Mesh combinedMesh = GetCombinedMesh(baseSprite, dummySprite, textureInfo, data, true);
            try
            {
                baseSprite.OverrideGeometry(combinedMesh.vertices.ToVector2(), combinedMesh.triangles.ToUShort());
            }
            finally
            {
                Object.DestroyImmediate(combinedMesh);
            }
        }

        public override GameObject CreateExternalObject(Sprite sprite, TextureInfo textureInfo, SpriteConfigData data, string oldPrefabPath = null)
        {
            return PrefabUtil.CreateMeshPrefab(textureInfo, false);
        }

        public override void CreateImportMeshes(Sprite importedSprite, Sprite dummySprite, TextureInfo textureInfo, SpriteConfigData data, out Mesh rootMesh, out Mesh subMesh)
        {
            // thickness はこの mode では適用しない（既存仕様）
            rootMesh = CreateMeshFromImportedSprite(importedSprite, textureInfo, data, applyThickness: false, RENDER_TYPE_OPAQUE);
            subMesh = null;
        }

        public override void UpdateExternalObject(GameObject externalObject, Sprite baseSprite, TextureInfo textureInfo, SpriteConfigData data)
        {
            PrefabUtil.UpdateMeshPrefab(textureInfo, false, externalObject);
            SpriteMeshAssets.TryGetMeshes(textureInfo.textureAssetPath, out Mesh rootMesh, out _);
            PrefabUtil.AddComponentsAssets(baseSprite, externalObject, rootMesh, RENDER_TYPE_OPAQUE, data.opaqueShaderName, data);
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
            Mesh opaqueMesh = null;
            Mesh gridMesh = null;
            Mesh combinedMesh = null;
            try
            {
                dummySprite.GetVertexAndTriangle2D(data, out var vertices, out var triangles, MeshRenderType.OpaqueWithoutTightGrid);
                if (applyPixelPerUnitScale) vertices = MeshUtil.GetScaledVertices(vertices, textureInfo, isClamped: true);
                opaqueMesh = MeshUtil.Update(null, vertices.ToVector3(), triangles.ToInt(), textureInfo, false);

                dummySprite.GetVertexAndTriangle2D(data, out var verticesGrid, out var trianglesGrid, MeshRenderType.TightGrid);
                if (applyPixelPerUnitScale) verticesGrid = MeshUtil.GetScaledVertices(verticesGrid, textureInfo, isClamped: true);
                gridMesh = MeshUtil.Update(null, verticesGrid.ToVector3(), trianglesGrid.ToInt(), textureInfo, false);

                combinedMesh = new Mesh();
                combinedMesh.CombineMeshes(new[]
                {
                    new CombineInstance { mesh = opaqueMesh, transform = Matrix4x4.identity },
                    new CombineInstance { mesh = gridMesh, transform = Matrix4x4.identity, }
                }, true);

                return combinedMesh;
            }
            catch
            {
                if (combinedMesh != null) Object.DestroyImmediate(combinedMesh);
                throw;
            }
            finally
            {
                if (opaqueMesh != null) Object.DestroyImmediate(opaqueMesh);
                if (gridMesh != null) Object.DestroyImmediate(gridMesh);
            }
        }
    }
}
