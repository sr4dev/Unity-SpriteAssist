using UnityEngine;

namespace OptSprite
{
    public class DefaultMeshCreator : MeshCreator
    {
        public override void OverrideGeometry(Sprite sprite, SpriteConfigData configData)
        {
            //use unity native mesh
        }

        public override GameObject CreateExternalObject(Sprite sprite, SpriteConfigData data)
        {
            GameObject prefab = CreateAndSavePrefab(sprite, false);
            AddComponentsAssets(sprite, data, prefab, RENDER_TYPE_TRANSPARENT, RENDER_SHADER_TRANSPARENT, MeshRenderType.Transparent);
            return prefab;
        }
    }

    public class TransparentMeshCreator : MeshCreator
    {
        public override void OverrideGeometry(Sprite sprite, SpriteConfigData configData)
        {
            SpriteUtil.GetMeshData(sprite, configData, out var vertices, out var triangles, MeshRenderType.Transparent);
            SpriteUtil.GetScaledVertices(vertices, sprite.pixelsPerUnit, sprite.pivot, sprite.rect.size, 1, false, false);
            sprite.OverrideGeometry(vertices, triangles);
        }

        public override GameObject CreateExternalObject(Sprite sprite, SpriteConfigData data)
        {
            GameObject prefab = CreateAndSavePrefab(sprite, false);
            AddComponentsAssets(sprite, data, prefab, RENDER_TYPE_TRANSPARENT, RENDER_SHADER_TRANSPARENT, MeshRenderType.Transparent);
            return prefab;
        }
    }

    public class OpaqueMeshCreator : MeshCreator
    {
        public override void OverrideGeometry(Sprite sprite, SpriteConfigData configData)
        {
            SpriteUtil.GetMeshData(sprite, configData, out var vertices, out var triangles, MeshRenderType.Opaque);
            SpriteUtil.GetScaledVertices(vertices, sprite.pixelsPerUnit, sprite.pivot, sprite.rect.size, 1, false, false);
            sprite.OverrideGeometry(vertices, triangles);
        }

        public override GameObject CreateExternalObject(Sprite sprite, SpriteConfigData data)
        {
            GameObject prefab = CreateAndSavePrefab(sprite, false);
            AddComponentsAssets(sprite, data, prefab, RENDER_TYPE_OPAQUE, RENDER_SHADER_OPAQUE, MeshRenderType.Opaque);
            return prefab;
        }
    }

    public class ComplexMeshCreator : MeshCreator
    {
        public override void OverrideGeometry(Sprite sprite, SpriteConfigData configData)
        {
            //does not supported
        }

        public override GameObject CreateExternalObject(Sprite sprite, SpriteConfigData data)
        {
            GameObject root = CreateAndSavePrefab(sprite, true);
            GameObject sub = root.transform.GetChild(0).gameObject;
            AddComponentsAssets(sprite, data, root, RENDER_TYPE_TRANSPARENT, RENDER_SHADER_TRANSPARENT, MeshRenderType.SeparatedTransparent);
            AddComponentsAssets(sprite, data, sub, RENDER_TYPE_OPAQUE, RENDER_SHADER_OPAQUE, MeshRenderType.Opaque);
            return root;
        }
    }

}