using System.Collections.Generic;
using UnityEngine;

namespace SpriteAssist
{
    public abstract class MeshCreatorBase
    {
        public const string RENDER_TYPE_TRANSPARENT = "Transparent";
        public const string RENDER_TYPE_OPAQUE = "Opaque";

        private static readonly IReadOnlyDictionary<SpriteConfigData.Mode, MeshCreatorBase> _creator = new Dictionary<SpriteConfigData.Mode, MeshCreatorBase>()
        {
            { SpriteConfigData.Mode.UnityDefaultForTransparent, new DefaultTransparentMeshCreator() },
            { SpriteConfigData.Mode.UnityDefaultForOpaque, new DefaultOpaqueMeshCreator() },
            { SpriteConfigData.Mode.TransparentMesh, new TransparentMeshCreator() },
            { SpriteConfigData.Mode.OpaqueMesh, new OpaqueMeshCreator() },
            { SpriteConfigData.Mode.ComplexMesh, new ComplexMeshCreator() },
            { SpriteConfigData.Mode.GridMesh, new GridMeshCreator() },
            { SpriteConfigData.Mode.OpaqueEdgeGridMesh, new OpaqueEdgeGridMeshCreator() },
            { SpriteConfigData.Mode.PixelMesh, new PixelMeshCreator() }
        };

        public static MeshCreatorBase GetInstance(SpriteConfigData.Mode mode)
        {
            return _creator[mode];
        }

        public abstract void OverrideGeometry(Sprite baseSprite, Sprite dummySprite, TextureInfo textureInfo, SpriteConfigData data);

        public abstract GameObject CreateExternalObject(Sprite sprite, TextureInfo textureInfo, SpriteConfigData data, string oldPrefabPath = null);

        // Called during import. Generate and return Mesh Prefab Meshes for output as texture subassets.
        // importedSprite is the Sprite after OverrideGeometry.
        public abstract void CreateImportMeshes(Sprite importedSprite, Sprite dummySprite, TextureInfo textureInfo, SpriteConfigData data, out Mesh rootMesh, out Mesh subMesh);

        // Update Mesh Prefab structure, Materials, and MeshFilter references only for user actions, never during import.
        public abstract void UpdateExternalObject(GameObject externalObject, Sprite baseSprite, TextureInfo textureInfo, SpriteConfigData data);

        public abstract List<SpritePreviewWireframe> GetMeshWireframes();

        // Create a Mesh from the Sprite geometry after OverrideGeometry.
        // Tests verify equivalence to retriangulating the dummy sprite.
        protected static Mesh CreateMeshFromImportedSprite(Sprite importedSprite, TextureInfo textureInfo, SpriteConfigData data, bool applyThickness, string name)
        {
            Vector3[] vertices = importedSprite.vertices.ToVector3();
            int[] triangles = importedSprite.triangles.ToInt();

            if (applyThickness && data.thickness > 0)
            {
                TriangulationUtil.ExpandMeshThickness(ref vertices, ref triangles, data.thickness);
            }

            Mesh mesh = MeshUtil.Update(null, vertices, triangles, textureInfo, data.isCorrectNormal, data.isWeldVertices);
            mesh.name = name;
            return mesh;
        }
    }
}
