using System;
using UnityEngine;

namespace SpriteAssist
{
    [Serializable]
    public sealed class MeshUvTransform
    {
        public int version = 1;
        public Vector3 u = new Vector3(1, 0, 0);
        public Vector3 v = new Vector3(0, 1, 0);

        public Vector2 Evaluate(Vector2 point)
        {
            if (version != 1) throw new InvalidOperationException("Unsupported SpriteAssist UV input version");
            var result = new Vector2(u.x * point.x + u.y * point.y + u.z,
                v.x * point.x + v.y * point.y + v.z);
            if (float.IsNaN(result.x) || float.IsInfinity(result.x) || float.IsNaN(result.y) || float.IsInfinity(result.y))
                throw new InvalidOperationException("Invalid SpriteAssist UV input");
            return result;
        }
    }
}
