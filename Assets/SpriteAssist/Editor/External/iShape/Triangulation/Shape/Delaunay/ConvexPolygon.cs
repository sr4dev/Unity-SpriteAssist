using iShape.Collections;
using iShape.Geometry;
using iShape.Geometry.Container;
using iShape.Geometry.Polygon;
using Unity.Collections;

namespace iShape.Triangulation.Shape.Delaunay {

    internal struct ConvexPolygon {
        internal readonly struct Edge {
            internal readonly int triangleIndex;
            internal readonly int neighbor;
            internal readonly int a;
            internal readonly int b;

            internal Edge(int triangleIndex, int neighbor, int a, int b) {
                this.triangleIndex = triangleIndex;
                this.neighbor = neighbor;
                this.a = a;
                this.b = b;
            }
        }

        internal DynamicArray<Edge> edges;
        private DynamicArray<Link> links;

        internal NativeArray<IntVector> Points(Allocator allocator) {
            int n = this.links.Count;
            var result = new NativeArray<IntVector>(n, allocator);
            for (int i = 0; i < n; ++i) {
                result[i] = this.links[i].vertex.point;
            }

            return result;
        }

        internal bool Add(Edge edge, Triangle triangle) {
            var v = triangle.OppositeVertex(edge.triangleIndex);

            // a0 -> a1 -> p
            var link_a1 = this.links[edge.a];
            var va0 = this.links[link_a1.prev].vertex;
            var va1 = link_a1.vertex;

            var aa = va1.point - va0.point;
            var ap = v.point - va1.point;

            var apa = aa.CrossProduct(ap);
            if (apa > 0) {
                return false;
            }

            // b0 <- b1 <- p

            var link_b1 = this.links[edge.b];
            var vb0 = this.links[link_b1.next].vertex;
            var vb1 = link_b1.vertex;

            var bb = vb0.point - vb1.point;
            var bp = vb1.point - v.point;

            var bpb = bp.CrossProduct(bb);
            if (bpb > 0) {
                return false;
            }

            var linkIndex = this.links.Count;
            var link_p = new Link(link_a1.self, linkIndex, link_b1.self, v);

            link_a1.next = linkIndex;
            link_b1.prev = linkIndex;

            this.links.Add(link_p);
            this.links[link_a1.self] = link_a1;
            this.links[link_b1.self] = link_b1;

            var n0 = triangle.FindNeighbor(vb1.index);
            if (n0 >= 0) {
                var edge0 = new Edge(triangle.index, n0, edge.a, linkIndex);
                this.edges.Add(edge0);
            }

            var n1 = triangle.FindNeighbor(va1.index);
            if (n1 >= 0) {
                var edge1 = new Edge(triangle.index, n1, linkIndex, edge.b);
                this.edges.Add(edge1);
            }

            return true;
        }

        internal ConvexPolygon(Triangle triangle) {
            const int capacity = 16;

            this.links = new DynamicArray<Link>(capacity, Allocator.Temp);
            this.links.Add(new Link(2, 0, 1, triangle.Vertex(0)));
            this.links.Add(new Link(0, 1, 2, triangle.Vertex(1)));
            this.links.Add(new Link(1, 2, 0, triangle.Vertex(2)));

            this.edges = new DynamicArray<Edge>(capacity, Allocator.Temp);

            var ab = triangle.Neighbor(2);
            if (ab >= 0) {
                this.edges.Add(new Edge(triangle.index, ab, 0, 1));
            }

            var bc = triangle.Neighbor(0);
            if (bc >= 0) {
                this.edges.Add(new Edge(triangle.index, bc, 1, 2));
            }

            var ca = triangle.Neighbor(1);
            if (ca >= 0) {
                this.edges.Add(new Edge(triangle.index, ca, 2, 0));
            }
        }

        internal void Dispose() {
            this.edges.Dispose();
            this.links.Dispose();
        }
    }

    public static class Polygons {

        public static List ConvexPolygons(this PlainShape self, Allocator allocator, IntGeom intGeom) {
            var delaunay = self.Delaunay(Allocator.Temp);
            var list = delaunay.ConvexPolygons(intGeom, allocator);
            delaunay.Dispose();
            return list;
        }
        
        public static List ConvexPolygons(this Delaunay self, IntGeom intGeom, Allocator allocator) {
            int n = self.triangles.Count;
            var dynamicList = new DynamicList(self.points.Count, n >> 1, allocator);
            var visited = new NativeArray<bool>(n, Allocator.Temp);
        
            for (int i = 0; i < n; ++i) {
                if (visited[i]) {
                    continue;
                }

                var first = self.triangles[i];
                visited[i] = true;
                var convexPolygon = new ConvexPolygon(first);

                while (convexPolygon.edges.Count > 0) {
                    var edge = convexPolygon.edges.Last();
                    convexPolygon.edges.RemoveLast();
                    if (visited[edge.neighbor]) {
                        continue;
                    }

                    var next = self.triangles[edge.neighbor];
                    if (convexPolygon.Add(edge, next)) {
                        visited[edge.neighbor] = true;
                    }
                }

                var iPoints = convexPolygon.Points(Allocator.Temp);
                var points = intGeom.Float(iPoints, Allocator.Temp);
                var polygon = new Polygon(points, allocator);
                dynamicList.Add(polygon);

                iPoints.Dispose();
                points.Dispose();
                convexPolygon.Dispose();
            }
            
            visited.Dispose();

            return dynamicList.Convert();
        }
    }

}