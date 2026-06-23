using Unity.Collections;
using UnityEngine;

namespace iShape.Geometry.Polygon {

    public static class PolygonExt {

        public static Vector2 GetCentralSymmetry(this NativeSlice<Vector2> self) {
            float x = 0;
            float y = 0;
            int n = self.Length;
            var b = self[n - 1];
            for (int i = 0; i < n; ++i) {
                var a = self[i]; 

                float d = a.x * b.y - b.x * a.y;
                x += (a.x + b.x) * d;
                y += (a.y + b.y) * d;

                b = a;
            }

            float k = 1f / (6f * Area(self));

            return new Vector2(k * x, k * y);
        }
        
        public static Vector2 GetCentralSymmetry(this NativeSlice<Vector2> self, float area) {
            float x = 0;
            float y = 0;
            int n = self.Length;
            var b = self[n - 1];
            for (int i = 0; i < n; ++i) {
                var a = self[i]; 

                float d = a.x * b.y - b.x * a.y;
                x += (a.x + b.x) * d;
                y += (a.y + b.y) * d;

                b = a;
            }

            float k = 1f / (6f * area);

            return new Vector2(k * x, k * y);
        }
        
        public static float Area(this NativeSlice<Vector2> self) {
            int n = self.Length;
            float sum = 0f;
            var p1 = self[n - 1];
            for (int i = 0; i < n; i++) {
                var p2 = self[i];
                float dif_x = p2.x - p1.x;
                float sum_y = p2.y + p1.y;
                sum += dif_x * sum_y;
                p1 = p2;
            }

            return 0.5f * sum;
        }
    }

}