using UnityEngine;

namespace SpriteAssist
{
    public abstract class SingleMeshCreatorBase : MeshCreatorBase
    {
        protected abstract MeshRenderType MeshRenderType3D { get; }

        protected abstract string RenderType { get; }

        protected abstract string GetShaderName(SpriteConfigData data);

        public override void OverrideGeometry(Sprite baseSprite, Sprite dummySprite, TextureInfo textureInfo, SpriteConfigData data)
        {
            dummySprite.GetVertexAndTriangle2D(data, out var vertices, out var triangles, MeshRenderType3D);
            vertices = MeshUtil.GetScaledVertices(vertices, textureInfo, isClamped: true);
            baseSprite.OverrideGeometry(vertices, triangles);
        }

        public override GameObject CreateExternalObject(Sprite sprite, TextureInfo textureInfo, SpriteConfigData data, string oldPrefabPath = null)
        {
            return PrefabUtil.CreateMeshPrefab(textureInfo, false);
        }

        public override void CreateImportMeshes(Sprite importedSprite, Sprite dummySprite, TextureInfo textureInfo, SpriteConfigData data, out Mesh rootMesh, out Mesh subMesh)
        {
            rootMesh = CreateMeshFromImportedSprite(importedSprite, textureInfo, data, applyThickness: true, RenderType);
            subMesh = null;
        }

        public override void UpdateExternalObject(GameObject externalObject, Sprite baseSprite, TextureInfo textureInfo, SpriteConfigData data)
        {
            PrefabUtil.UpdateMeshPrefab(textureInfo, false, externalObject);
            SpriteMeshAssets.TryGetMeshes(textureInfo.textureAssetPath, out Mesh rootMesh, out _);
            PrefabUtil.AddComponentsAssets(baseSprite, externalObject, rootMesh, RenderType, GetShaderName(data), data);
        }
    }
}
