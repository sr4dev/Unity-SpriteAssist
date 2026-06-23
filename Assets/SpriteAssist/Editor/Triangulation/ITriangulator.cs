using UnityEngine;

namespace SpriteAssist
{
    internal interface ITriangulator
    {
        string DisplayName { get; }

        string Description { get; }

        bool TryTriangulate(
            SpriteConfigData config,
            Vector2[][] paths,
            out Vector2[] vertices,
            out ushort[] triangles);
    }
}
