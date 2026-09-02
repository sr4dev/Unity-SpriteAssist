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

        // import 中に呼ばれる。Mesh Prefab 用の Mesh を生成して返す（テクスチャのサブアセットとして出力される）。
        // importedSprite は OverrideGeometry 適用後のスプライト。
        public abstract void CreateImportMeshes(Sprite importedSprite, Sprite dummySprite, TextureInfo textureInfo, SpriteConfigData data, out Mesh rootMesh, out Mesh subMesh);

        // Mesh Prefab の構造・Material・MeshFilter 参照を更新する（ユーザー操作時のみ。import 中には呼ばない）
        public abstract void UpdateExternalObject(GameObject externalObject, Sprite baseSprite, TextureInfo textureInfo, SpriteConfigData data);

        public abstract List<SpritePreviewWireframe> GetMeshWireframes();

        // OverrideGeometry 済みスプライトのジオメトリから Mesh を作る。
        // dummy sprite からの再三角形分割と等価であることはテストで検証済み。
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
