using UnityEngine;

namespace OptSprite
{
    public class DefaultMeshPrefabCreator : MeshPrefabCreator
    {
        public override void OverrideGeometry(Sprite sprite, SpriteConfigData configData)
        {
            //use unity native mesh
        }

        public override GameObject CreateExternalObject(Sprite sprite, SpriteConfigData data)
        {
            GameObject prefab = CreateAndSavePrefab(sprite, false);
            AddComponentsAssets(sprite, data, prefab, "Transparent", "Unlit/Transparent", MeshRenderType.Transparent);
            return prefab;
        }
    }

    public class TransparentMeshPrefabCreator : MeshPrefabCreator
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
            AddComponentsAssets(sprite, data, prefab, "Transparent", "Unlit/Transparent", MeshRenderType.Transparent);
            return prefab;
        }
    }

    public class OpaqueMeshPrefabCreator : MeshPrefabCreator
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
            AddComponentsAssets(sprite, data, prefab, "Opaque", "Unlit/Texture", MeshRenderType.Opaque);
            return prefab;
        }
    }

    public class ComplexMeshPrefabCreator : MeshPrefabCreator
    {
        public override void OverrideGeometry(Sprite sprite, SpriteConfigData configData)
        {
            //does not supported
        }

        public override GameObject CreateExternalObject(Sprite sprite, SpriteConfigData data)
        {
            GameObject root = CreateAndSavePrefab(sprite, true);
            GameObject sub = root.transform.GetChild(0).gameObject;
            AddComponentsAssets(sprite, data, root, "Transparent", "Unlit/Transparent", MeshRenderType.SeparatedTransparent);
            AddComponentsAssets(sprite, data, sub, "Opaque", "Unlit/Texture", MeshRenderType.Opaque);
            return root;
        }
    }

}