using UnityEngine;

namespace SpriteAssist
{
    internal interface ITriangulator
    {
        bool TryTriangulate(
            SpriteConfigData config,
            Vector2[][] paths,
            out Vector2[] vertices,
            out ushort[] triangles);
    }
}
