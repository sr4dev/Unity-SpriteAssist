using iShape.Triangulation.Util;
using iShape.Geometry;
using iShape.Geometry.Container;
using Unity.Collections;
using UnityEngine;

namespace iShape.Triangulation.Shape.Delaunay {

    public static class Triangulation {

        public static Mesh DelaunayTriangulate(this PlainShape shape, IntGeom iGeom) {
            int n = shape.points.Length;
            var vertices = new Vector3[n];
            for (int i = 0; i < n; ++i) {
                var v = iGeom.Float(shape.points[i]);
                vertices[i] = new Vector3(v.x, v.y, 0);
            }
            var extraPoints = new NativeArray<IntVector>(0, Allocator.Temp);
            var delaunay = shape.Delaunay(0, extraPoints, Allocator.Temp);
            extraPoints.Dispose();
            
            var nTriangles = delaunay.Indices(Allocator.Temp);
            delaunay.Dispose();
            
            var mesh = new Mesh {
                vertices = vertices,
                triangles = nTriangles.ToArray()
            };
            
            nTriangles.Dispose();
            
            return mesh;
        }
        
        public static NativeArray<int> DelaunayTriangulate(this PlainShape shape, Allocator allocator) {
            var extraPoints = new NativeArray<IntVector>(0, Allocator.Temp);
            var delaunay = shape.Delaunay(0, extraPoints, allocator);
            extraPoints.Dispose();
            
            var triangles = delaunay.Indices(allocator);
            delaunay.Dispose();
            
            return triangles;
        }
        
        public static NativeArray<int> DelaunayTriangulate(this PlainShape shape, Allocator allocator, NativeArray<IntVector> extraPoints) {
            var delaunay = shape.Delaunay(0, extraPoints, allocator);
            var triangles = delaunay.Indices(allocator);
            delaunay.Dispose();
            
            return triangles;
        }
        
        public static Delaunay Delaunay(this PlainShape shape, long maxEdge, NativeArray<IntVector> extraPoints, Allocator allocator) {
            var layout = shape.Split(maxEdge, extraPoints, Allocator.Temp);

            int holesCount = shape.layouts.Length;
            int totalCount = layout.pathCount + 2 * layout.extraCount + holesCount * 2 - 2;

            var triangleStack = new TriangleStack(totalCount, allocator);

            for(int i = 0; i < layout.indices.Length; ++i) {
                int index = layout.indices[i];
                Triangulate(index, layout.links, ref triangleStack);
                triangleStack.Reset();
            }

            var triangles = triangleStack.Convert();

            var sliceBuffer = new SliceBuffer(layout.links.Length, layout.slices, Allocator.Temp);
            sliceBuffer.AddConnections(triangles);

            sliceBuffer.Dispose();

            Delaunay delaunay;
            if (extraPoints.Length == 0 && maxEdge == 0) {
                delaunay = new Delaunay(shape.points, triangles, allocator);
            } else {
                var points = new NativeArray<IntVector>(layout.links.Length, Allocator.Temp);
                for(int i = 0; i < layout.links.Length; ++i) {
                    var link = layout.links[i];
                    points[link.vertex.index] = link.vertex.point;
                }
                delaunay = new Delaunay(points, triangles, allocator);
                points.Dispose();
            }
            
            triangles.Dispose();

            layout.Dispose();
            
            delaunay.Build();
            
            return delaunay;
        }
        
        public static Delaunay Delaunay(this PlainShape shape, long maxEdge, Allocator allocator) {
            var extraPoints = new NativeArray<IntVector>(0, Allocator.Temp);
            var delaunay = shape.Delaunay(maxEdge, extraPoints, allocator);
            extraPoints.Dispose();
            return delaunay;
        }
        
        public static Delaunay Delaunay(this PlainShape shape, Allocator allocator) {
            var extraPoints = new NativeArray<IntVector>(0, Allocator.Temp);
            var delaunay = shape.Delaunay(0, extraPoints, allocator);
            extraPoints.Dispose();
            return delaunay;
        }

        private static void Triangulate(int index, NativeArray<Link> links, ref TriangleStack triangleStack) {

            var c = links[index];

            var a0 = links[c.next];
            var b0 = links[c.prev];

            while(a0.self != b0.self) {
                var a1 = links[a0.next];
                var b1 = links[b0.prev];


                var aBit0 = a0.vertex.point.BitPack;
                var aBit1 = a1.vertex.point.BitPack;
                if(aBit1 < aBit0) {
                    aBit1 = aBit0;
                }

                var bBit0 = b0.vertex.point.BitPack;
                var bBit1 = b1.vertex.point.BitPack;
                if(bBit1 < bBit0) {
                    bBit1 = bBit0;
                }

                if(aBit0 <= bBit1 && bBit0 <= aBit1) {
                    triangleStack.Add(c.vertex, a0.vertex, b0.vertex);

                    a0.prev = b0.self;
                    b0.next = a0.self;
                    links[a0.self] = a0;
                    links[b0.self] = b0;

                    if(bBit0 < aBit0) {
                        c = b0;
                        b0 = b1;
                    } else {
                        c = a0;
                        a0 = a1;
                    }

                } else {
                    if(aBit1 < bBit1) {
                        var cx = c;
                        var ax0 = a0;
                        var ax1 = a1;
                        long ax1Bit;
                        do {
                            var isCCW_or_Line = IntTriangle.IsCCW_or_Line(cx.vertex.point, ax0.vertex.point, ax1.vertex.point);

                            if(isCCW_or_Line) {
                                triangleStack.Add(ax0.vertex, ax1.vertex, cx.vertex);

                                ax1.prev = cx.self;
                                cx.next = ax1.self;
                                links[cx.self] = cx;
                                links[ax1.self] = ax1;

                                if(cx.self != c.self) {
                                    // move back
                                    ax0 = cx;
                                    cx = links[cx.prev];
                                } else {
                                    // move forward
                                    ax0 = ax1;
                                    ax1 = links[ax1.next];
                                }
                            } else {
                                cx = ax0;
                                ax0 = ax1;
                                ax1 = links[ax1.next];
                            }
                            ax1Bit = ax1.vertex.point.BitPack;
                        } while(ax1Bit < bBit0);
                    } else {
                        var cx = c;
                        var bx0 = b0;
                        var bx1 = b1;
                        long bx1Bit;
                        do {
                            bool isCCW_or_Line = IntTriangle.IsCCW_or_Line(cx.vertex.point, bx1.vertex.point, bx0.vertex.point);
                            if(isCCW_or_Line) {
                                triangleStack.Add(bx0.vertex, cx.vertex, bx1.vertex);

                                bx1.next = cx.self;
                                cx.prev = bx1.self;
                                links[cx.self] = cx;
                                links[bx1.self] = bx1;

                                if(cx.self != c.self) {
                                    // move back
                                    bx0 = cx;
                                    cx = links[cx.next];
                                } else {
                                    // move forward
                                    bx0 = bx1;
                                    bx1 = links[bx0.prev];
                                }
                            } else {
                                cx = bx0;
                                bx0 = bx1;
                                bx1 = links[bx1.prev];
                            }
                            bx1Bit = bx1.vertex.point.BitPack;
                        } while(bx1Bit < aBit0);
                    }

                    c = links[c.self];
                    a0 = links[c.next];
                    b0 = links[c.prev];

                    aBit0 = a0.vertex.point.BitPack;
                    bBit0 = b0.vertex.point.BitPack;

                    triangleStack.Add(c.vertex, a0.vertex, b0.vertex);

                    a0.prev = b0.self;
                    b0.next = a0.self;
                    links[a0.self] = a0;
                    links[b0.self] = b0;

                    if(bBit0 < aBit0) {
                        c = b0;
                        b0 = links[b0.prev];
                    } else {
                        c = a0;
                        a0 = links[a0.next];
                    }

                }
            } // while
        }
    }

}
