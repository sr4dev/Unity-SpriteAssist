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
            { SpriteConfigData.Mode.GridMesh, new GridMeshCreator() }
        };

        public static MeshCreatorBase GetInstance(SpriteConfigData.Mode mode)
        {
            return _creator[mode];
        }

        public abstract void OverrideGeometry(Sprite baseSprite, Sprite dummySprite, TextureInfo textureInfo, SpriteConfigData configData);

        public abstract GameObject CreateExternalObject(Sprite sprite, TextureInfo textureInfo, SpriteConfigData data, string oldPrefabPath = null);

        public abstract void UpdateExternalObject(GameObject externalObject, Sprite baseSprite, Sprite dummySprite, TextureInfo textureInfo, SpriteConfigData data);

        public abstract List<SpritePreviewWireframe> GetMeshWireframes();
    }
}