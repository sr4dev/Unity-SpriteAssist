using System.Collections.Generic;
using UnityEngine;

namespace SpriteAssist
{
    public abstract class MeshCreator
    {
        public const string RENDER_TYPE_TRANSPARENT = "Transparent";
        public const string RENDER_TYPE_OPAQUE = "Opaque";

        public const string RENDER_SHADER_TRANSPARENT = "Unlit/Transparent";
        public const string RENDER_SHADER_OPAQUE = "Unlit/Texture";

        private static readonly MeshCreator _defaultCreator = new DefaultMeshCreator();

        private static readonly IReadOnlyDictionary<SpriteConfigData.Mode, MeshCreator> _creator = new Dictionary<SpriteConfigData.Mode, MeshCreator>()
        {
            { SpriteConfigData.Mode.TransparentMesh, new TransparentMeshCreator() },
            { SpriteConfigData.Mode.OpaqueMesh, new OpaqueMeshCreator() },
            { SpriteConfigData.Mode.Complex, new ComplexMeshCreator() }
        };

        public static MeshCreator GetInstnace(SpriteConfigData configData)
        {
            return configData.overriden ? _creator[configData.mode] : _defaultCreator;
        }

        public abstract void OverrideGeometry(Sprite sprite, SpriteConfigData configData);

        public abstract GameObject CreateExternalObject(Sprite sprite, SpriteConfigData data);

        public abstract List<SpritePreviewWireframe> GetMeshWireframes();
    }
}