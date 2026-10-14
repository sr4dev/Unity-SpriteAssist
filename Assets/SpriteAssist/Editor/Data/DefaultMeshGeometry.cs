using UnityEngine;

namespace SpriteAssist
{
    // Owned by a single import. Sprite coordinate limits must not change the outline of the persistent Mesh.
    public sealed class DefaultMeshGeometry
    {
        private readonly Vector2[] _vertices;
        private readonly ushort[] _triangles;

        public DefaultMeshGeometry(Vector2[] vertices, ushort[] triangles)
        {
            _vertices = (Vector2[])vertices.Clone();
            _triangles = (ushort[])triangles.Clone();
        }

        public static DefaultMeshGeometry Generate(Sprite sprite, Sprite dummy, TextureInfo info, SpriteConfigData config)
        {
            var source = OutlineUtil.HasImporterOutline(sprite, info.textureAssetPath) ? sprite : dummy;
            var renderType = config.mode == SpriteConfigData.Mode.UnityDefaultForOpaque
                ? MeshRenderType.OpaqueWithoutExtrude : MeshRenderType.Transparent;
            source.GetVertexAndTriangle2D(config, out var vertices, out var triangles, renderType, info.textureAssetPath);
            return new DefaultMeshGeometry(vertices, triangles);
        }

        public void ApplyToSprite(Sprite sprite, TextureInfo info)
        {
            sprite.OverrideGeometry(GetSpritePixelVertices(info), _triangles);
        }

        public Vector2[] GetSpritePixelVertices(TextureInfo info) =>
            MeshUtil.GetScaledVertices((Vector2[])_vertices.Clone(), info, isClamped: true);

        public Mesh CreateMesh(TextureInfo info, SpriteConfigData config)
        {
            var vertices = _vertices.ToVector3();
            var triangles = _triangles.ToInt();
            if (config.thickness > 0) TriangulationUtil.ExpandMeshThickness(ref vertices, ref triangles, config.thickness);
            Vector2[] uv = null;
            if (config.meshUv != null)
            {
                uv = new Vector2[vertices.Length];
                for (int i = 0; i < uv.Length; i++)
                {
                    var pixel = new Vector2(vertices[i].x, vertices[i].y) * info.pixelPerUnit + info.pivot;
                    uv[i] = config.meshUv.Evaluate(new Vector2(pixel.x / info.rect.width, pixel.y / info.rect.height));
                }
            }
            var mesh = MeshUtil.Update(null, vertices, triangles, info, config.isCorrectNormal, config.isWeldVertices, uv);
            mesh.name = config.mode == SpriteConfigData.Mode.UnityDefaultForOpaque
                ? MeshCreatorBase.RENDER_TYPE_OPAQUE : MeshCreatorBase.RENDER_TYPE_TRANSPARENT;
            return mesh;
        }
    }
}
