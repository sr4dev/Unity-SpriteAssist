using UnityEngine;

namespace iShape.Geometry.Extension {
    
    public static class Vector2_Ext {

        public static float Multiply(this Vector2 self, Vector2 vector) {
            return self.x * vector.y - self.y * vector.x;
        }

        public static float SqrDistance(this Vector2 self, Vector2 vector) {
            float dx = vector.x - self.x;
            float dy = vector.y - self.y;

            return dx * dx + dy * dy;
        }
    }
}