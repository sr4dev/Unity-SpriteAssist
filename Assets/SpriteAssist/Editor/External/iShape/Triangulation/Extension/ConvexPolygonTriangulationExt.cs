using iShape.Geometry.Polygon;
using Unity.Collections;
using UnityEngine;

namespace iShape.Triangulation.Extension {

    public static class ConvexPolygonTriangulationExt {

        public static NativeArray<int> GetTriangles(this Polygon self, Allocator allocator) {
            int n = self.points.Length;
            var count = 3 * (n - 2);
            var triangles = new NativeArray<int>(count, allocator);
            for (int i = 2, j = 0; i < n; ++i, j += 3) {
                triangles[j] = 0;
                triangles[j + 1] = i - 1;
                triangles[j + 2] = i;
            }

            return triangles;
        }
        
        public static NativeArray<Vector3> GetVertices(this Polygon self, Allocator allocator) {
            int n = self.points.Length;
            var vertices = new NativeArray<Vector3>(n, allocator);
            for (int i = 0; i < n; ++i) {
                vertices[i] = self.points[i];
            }
            return vertices;
        }
    }

}