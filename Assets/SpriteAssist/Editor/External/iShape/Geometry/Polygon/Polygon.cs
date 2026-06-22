using Unity.Collections;
using UnityEngine;

namespace iShape.Geometry.Polygon {

    public readonly struct Form {
        public readonly Vector2 center;
        public readonly float area;

        public Form(Vector2 center, float area) {
            this.center = center;
            this.area = area;
        }
    }

    public readonly struct Polygon {

        public readonly Form form;
        public readonly NativeArray<Vector2> points;
        
        public Polygon(NativeArray<Vector2> points, Form form) {
            this.points = points;
            this.form = form;
        }
        
        public Polygon(NativeArray<Vector2> path, Allocator allocator) {
            var slice = path.Slice();
            float area = slice.Area();
            var center = slice.GetCentralSymmetry(area);
            int n = path.Length;
            this.points = new NativeArray<Vector2>(n, allocator);
            for(int i = 0; i < n; ++i) {
                points[i] = path[i] - center;
            }

            this.form = new Form(center, area);
        }

        public NativeArray<Vector3> Vertices(Allocator allocator, float z = 0) {
            int n = points.Length;
            var result = new NativeArray<Vector3>(n, allocator);
            for (int i = 0; i < n; ++i) {
                var p = points[i];
                result[i] = new Vector3(p.x, p.y, z);
            }

            return result;
        }
        
        public NativeArray<int> ConvexIndices(Allocator allocator, int offset = 0) {
            int n = 3 * (points.Length - 2);
            var result = new NativeArray<int>(n, allocator);
            for (int i = 1, j = 0; i < points.Length - 1; ++i) {
                result[j++] = offset;
                result[j++] = i + offset;
                result[j++] = i + 1 + offset;
            }

            return result;
        }

        public void Dispose() {
            this.points.Dispose();
        }
    }

}