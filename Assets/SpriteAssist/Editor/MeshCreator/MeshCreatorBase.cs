using System.Collections.Generic;
using UnityEngine;

namespace SpriteAssist
{
    public abstract class MeshCreatorBase
    {
        public const string RENDER_TYPE_TRANSPARENT = "Transparent";
        public const string RENDER_TYPE_OPAQUE = "Opaque";

        private static readonly MeshCreatorBase _defaultCreator = new DefaultMeshCreator();

        private static readonly IReadOnlyDictionary<SpriteConfigData.Mode, MeshCreatorBase> _creator = new Dictionary<SpriteConfigData.Mode, MeshCreatorBase>()
        {
            { SpriteConfigData.Mode.TransparentMesh, new TransparentMeshCreator() },
            { SpriteConfigData.Mode.OpaqueMesh, new OpaqueMeshCreator() },
            { SpriteConfigData.Mode.Complex, new ComplexMeshCreator() }
        };

        public static MeshCreatorBase GetInstnace(SpriteConfigData configData)
        {
            return configData.IsOverriden ? _creator[configData.mode] : _defaultCreator;
        }

        public abstract void OverrideGeometry(Sprite sprite, TextureInfo textureInfo, SpriteConfigData configData);

        public abstract GameObject CreateExternalObject(Sprite sprite, TextureInfo textureInfo, SpriteConfigData data, string oldPrefabPath = null);

        public abstract List<SpritePreviewWireframe> GetMeshWireframes();
    }
}