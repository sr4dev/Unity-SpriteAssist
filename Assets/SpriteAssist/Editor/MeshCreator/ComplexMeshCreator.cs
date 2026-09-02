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

        public override void CreateImportMeshes(Sprite importedSprite, Sprite dummySprite, TextureInfo textureInfo, SpriteConfigData data, out Mesh rootMesh, out Mesh subMesh)
        {
            dummySprite.GetVertexAndTriangle3D(data, out var transparentVertices, out var transparentTriangles, MeshRenderType.SeparatedTransparent);
            dummySprite.GetVertexAndTriangle3D(data, out var opaqueVertices, out var opaqueTriangles, MeshRenderType.Opaque);

            rootMesh = MeshUtil.Update(null, transparentVertices, transparentTriangles, textureInfo, data.isCorrectNormal, data.isWeldVertices);
            rootMesh.name = RENDER_TYPE_TRANSPARENT;
            subMesh = MeshUtil.Update(null, opaqueVertices, opaqueTriangles, textureInfo, data.isCorrectNormal, data.isWeldVertices);
            subMesh.name = RENDER_TYPE_OPAQUE;
        }

        public override void UpdateExternalObject(GameObject externalObject, Sprite baseSprite, TextureInfo textureInfo, SpriteConfigData data)
        {
            PrefabUtil.UpdateMeshPrefab(textureInfo, true, externalObject);

            GameObject root = externalObject;
            GameObject sub = root.transform.GetChild(0).gameObject;
            SpriteMeshAssets.TryGetMeshes(textureInfo.textureAssetPath, out Mesh rootMesh, out Mesh subMesh);
            PrefabUtil.AddComponentsAssets(baseSprite, root, rootMesh, RENDER_TYPE_TRANSPARENT, data.transparentShaderName, data);
            PrefabUtil.AddComponentsAssets(baseSprite, sub, subMesh, RENDER_TYPE_OPAQUE, data.opaqueShaderName, data);
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
