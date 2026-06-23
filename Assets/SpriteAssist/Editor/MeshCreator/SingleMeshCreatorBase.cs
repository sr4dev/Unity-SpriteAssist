using UnityEngine;

namespace SpriteAssist
{
    public abstract class SingleMeshCreatorBase : MeshCreatorBase
    {
        protected abstract MeshRenderType MeshRenderType3D { get; }

        protected abstract string RenderType { get; }

        protected abstract string GetShaderName(SpriteConfigData data);

        protected virtual Sprite GetSource3D(Sprite baseSprite, Sprite dummySprite)
        {
            return dummySprite;
        }

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

        public override void UpdateExternalObject(GameObject externalObject, Sprite baseSprite, Sprite dummySprite, TextureInfo textureInfo, SpriteConfigData data)
        {
            PrefabUtil.UpdateMeshPrefab(textureInfo, false, externalObject);

            GetSource3D(baseSprite, dummySprite).GetVertexAndTriangle3D(data, out var vertices, out var triangles, MeshRenderType3D);
            PrefabUtil.AddComponentsAssets(baseSprite, externalObject, vertices, triangles, textureInfo, RenderType, GetShaderName(data), data);
        }

        public override void UpdateMeshInMeshPrefab(GameObject externalObject, Sprite baseSprite, Sprite dummySprite, TextureInfo textureInfo, SpriteConfigData data)
        {
            GetSource3D(baseSprite, dummySprite).GetVertexAndTriangle3D(data, out var vertices, out var triangles, MeshRenderType3D);
            PrefabUtil.UpdateMeshFiltersMesh(externalObject, vertices, triangles, textureInfo, data.isCorrectNormal, data.isWeldVertices);
        }
    }
}
