﻿using System.Collections.Generic;
using UnityEngine;

namespace SpriteAssist
{
    public class OpaqueMeshCreator : MeshCreatorBase
    {
        public override void OverrideGeometry(Sprite sprite, TextureInfo textureInfo, SpriteConfigData data)
        {
            sprite.GetVertexAndTriangle2D(data, out var vertices, out var triangles, MeshRenderType.Opaque);
            vertices = MeshUtil.GetScaledVertices(vertices, textureInfo, isClamped: true);
            sprite.OverrideGeometry(vertices, triangles);
        }

        public override GameObject CreateExternalObject(Sprite sprite, TextureInfo textureInfo, SpriteConfigData data, string oldPrefabPath = null)
        {
            return PrefabUtil.CreateMeshPrefab(textureInfo, false);
        }

        public override void UpdateExternalObject(GameObject externalObject, Sprite sprite, TextureInfo textureInfo, SpriteConfigData data)
        {
            PrefabUtil.UpdateMeshPrefab(textureInfo, false, externalObject);

            sprite.GetVertexAndTriangle3D(data, out var vertices, out var triangles, MeshRenderType.Opaque);
            PrefabUtil.AddComponentsAssets(sprite, externalObject, vertices, triangles, textureInfo, RENDER_TYPE_OPAQUE, data.opaqueShaderName, data);
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