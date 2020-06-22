using UnityEngine;

namespace OptSprite
{
    public class TransparentMeshPrefabCreator : MeshPrefabCreator
    {
        public override void OverrideGeometry(Sprite sprite, SpriteConfigData configData)
        {
            SpriteUtil.GetMeshData(sprite, configData, out var vertices, out var triangles, false, false);
            SpriteUtil.GetScaledVertices(vertices, sprite.pixelsPerUnit, sprite.pivot, sprite.rect.size, 1, false, false);
            sprite.OverrideGeometry(vertices, triangles);
        }

        public override GameObject CreateExternalObject(Sprite sprite, SpriteConfigData data)
        {
            GameObject prefab = CreateAndSavePrefab(sprite, false);
            AddComponentsAssets(sprite, data, prefab, "Transparent", "Unlit/Transparent", false, false);
            return prefab;
        }
    }

    public class OpaqueMeshPrefabCreator : MeshPrefabCreator
    {
        public override void OverrideGeometry(Sprite sprite, SpriteConfigData configData)
        {
            SpriteUtil.GetMeshData(sprite, configData, out var vertices, out var triangles, true, false);
            SpriteUtil.GetScaledVertices(vertices, sprite.pixelsPerUnit, sprite.pivot, sprite.rect.size, 1, false, false);
            sprite.OverrideGeometry(vertices, triangles);
        }

        public override GameObject CreateExternalObject(Sprite sprite, SpriteConfigData data)
        {
            GameObject prefab = CreateAndSavePrefab(sprite, false);
            AddComponentsAssets(sprite, data, prefab, "Opaque", "Unlit/Texture", true, false);
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
            AddComponentsAssets(sprite, data, root, "Transparent", "Unlit/Transparent", false, true);
            AddComponentsAssets(sprite, data, sub, "Opaque", "Unlit/Texture", true, false);
            return root;
        }
    }

}