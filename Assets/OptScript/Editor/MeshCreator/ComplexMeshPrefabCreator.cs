using System.Collections.Generic;
using UnityEngine;

namespace OptSprite
{
    public class DefaultMeshCreator : MeshCreator
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
            sprite.AddComponentsAssets(vertices, triangles, prefab, RENDER_TYPE_TRANSPARENT, RENDER_SHADER_TRANSPARENT);
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

    public class TransparentMeshCreator : MeshCreator
    {
        public override void OverrideGeometry(Sprite sprite, SpriteConfigData data)
        {
            sprite.GetMeshData(data, out var vertices, out var triangles, MeshRenderType.Transparent);
            sprite.SetSpriteScaleToVertices(vertices, 1, false, false);
            sprite.OverrideGeometry(vertices, triangles);
        }

        public override GameObject CreateExternalObject(Sprite sprite, SpriteConfigData data)
        {
            GameObject prefab = sprite.CreateEmptyMeshPrefab(false);
            sprite.GetMeshData(data, out var vertices, out var triangles, MeshRenderType.Transparent);
            sprite.AddComponentsAssets(vertices, triangles, prefab, RENDER_TYPE_TRANSPARENT, RENDER_SHADER_TRANSPARENT);
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

    public class OpaqueMeshCreator : MeshCreator
    {
        public override void OverrideGeometry(Sprite sprite, SpriteConfigData data)
        {
            sprite.GetMeshData(data, out var vertices, out var triangles, MeshRenderType.Opaque);
            sprite.SetSpriteScaleToVertices(vertices, 1, false, false);
            sprite.OverrideGeometry(vertices, triangles);
        }

        public override GameObject CreateExternalObject(Sprite sprite, SpriteConfigData data)
        {
            GameObject prefab = sprite.CreateEmptyMeshPrefab(false);
            sprite.GetMeshData(data, out var vertices, out var triangles, MeshRenderType.Opaque);
            sprite.AddComponentsAssets(vertices, triangles, prefab, RENDER_TYPE_OPAQUE, RENDER_SHADER_OPAQUE);
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

    public class ComplexMeshCreator : MeshCreator
    {
        public override void OverrideGeometry(Sprite sprite, SpriteConfigData data)
        {
            //does not supported
        }

        public override GameObject CreateExternalObject(Sprite sprite, SpriteConfigData data)
        {
            GameObject root = sprite.CreateEmptyMeshPrefab(true);
            GameObject sub = root.transform.GetChild(0).gameObject;
            sprite.GetMeshData(data, out var transparentVertices, out var transparentTriangles, MeshRenderType.SeparatedTransparent);
            sprite.GetMeshData(data, out var opaqueVertices, out var opaqueTriangles, MeshRenderType.Opaque);
            sprite.AddComponentsAssets(transparentVertices, transparentTriangles, root, RENDER_TYPE_TRANSPARENT, RENDER_SHADER_TRANSPARENT);
            sprite.AddComponentsAssets(opaqueVertices, opaqueTriangles, sub, RENDER_TYPE_OPAQUE, RENDER_SHADER_OPAQUE);
            return root;
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