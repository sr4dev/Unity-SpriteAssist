using iShape.Triangulation.Util;
using iShape.Geometry;
using iShape.Geometry.Container;
using Unity.Collections;
using UnityEngine;

namespace iShape.Triangulation.Shape {

    public static class TriangulationExt {

        public static Mesh Triangulate(this PlainShape shape, IntGeom iGeom) {
            int n = shape.points.Length;
            var vertices = new Vector3[n];
            for (int i = 0; i < n; ++i) {
                var v = iGeom.Float(shape.points[i]);
                vertices[i] = new Vector3(v.x, v.y, 0);
            }
            var extraPoints = new NativeArray<IntVector>(0, Allocator.Temp);
            var nTriangles = shape.Triangulate(extraPoints, Allocator.Temp);
            extraPoints.Dispose();

            var mesh = new Mesh {
                vertices = vertices,
                triangles = nTriangles.ToArray()
            };
            
            nTriangles.Dispose();
            
            return mesh;
        }
        
        public static NativeArray<int> Triangulate(this PlainShape shape, Allocator allocator) {
            var extraPoints = new NativeArray<IntVector>(0, Allocator.Temp);
            var triangles = Triangulate(shape, extraPoints, allocator);
            extraPoints.Dispose();
            return triangles;
        }

        public static NativeArray<int> Triangulate(this PlainShape shape, NativeArray<IntVector> extraPoints, Allocator allocator) {
            var layout = shape.Split(0, extraPoints, Allocator.Temp);
            int totalCount = shape.points.Length + ((shape.layouts.Length - 2) << 1);

			int trianglesCount = 3 * totalCount;

			var triangles = new NativeArray<int>(trianglesCount, allocator);
			int counter = 0;
			for(int i = 0; i < layout.indices.Length; ++i) {
                int index = layout.indices[i];
                Triangulate(index, ref counter, layout.links, triangles);
            }

            layout.Dispose();

			if(counter == trianglesCount) {
				return triangles;
			} else {
				var newTriangles = new NativeArray<int>(counter, allocator);
				newTriangles.Slice(0, counter).CopyFrom(triangles.Slice(0, counter));
				triangles.Dispose();
				return newTriangles;
			}
        }

        public static void Triangulate(int index, ref int counter, NativeArray<Link> links, NativeArray<int> triangles) {
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
                    if(IntTriangle.IsNotLine(c.vertex.point, a0.vertex.point, b0.vertex.point)) {
                        triangles[counter++] = c.vertex.index;
                        triangles[counter++] = a0.vertex.index;
                        triangles[counter++] = b0.vertex.index;
                    }

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
                        long ax1Bit = long.MinValue;
                        do {
                            var orientation = IntTriangle.GetOrientation(cx.vertex.point, ax0.vertex.point, ax1.vertex.point);
                            switch(orientation) {
                                case IntTriangle.Orientation.clockWise:
                                    triangles[counter++] = cx.vertex.index;
                                    triangles[counter++] = ax0.vertex.index;
                                    triangles[counter++] = ax1.vertex.index;
                                    goto case IntTriangle.Orientation.line;
                                case IntTriangle.Orientation.line:
                                    ax1.prev = cx.self;
                                    cx.next = ax1.self;
                                    links[cx.self] = cx;
                                    links[ax1.self] = ax1;

                                    if(cx.self != c.self) {
                                        // move back
                                        ax0 = cx;
                                        cx = links[cx.prev];
                                        continue;
                                    } else {
                                        // move forward
                                        ax0 = ax1;
                                        ax1 = links[ax1.next];
                                        break;
                                    }
                                case IntTriangle.Orientation.counterClockWise:
                                    cx = ax0;
                                    ax0 = ax1;
                                    ax1 = links[ax1.next];
                                    break;
                            }
                            ax1Bit = ax1.vertex.point.BitPack;
                        } while(ax1Bit < bBit0);
                    } else {
                        var cx = c;
                        var bx0 = b0;
                        var bx1 = b1;
                        long bx1Bit = long.MinValue;
                        do {
                            var orientation = IntTriangle.GetOrientation(cx.vertex.point, bx1.vertex.point, bx0.vertex.point);
                            switch(orientation) {
                                case IntTriangle.Orientation.clockWise:
                                    triangles[counter++] = cx.vertex.index;
                                    triangles[counter++] = bx1.vertex.index;
                                    triangles[counter++] = bx0.vertex.index;
                                    goto case IntTriangle.Orientation.line;
                                case IntTriangle.Orientation.line:
                                    bx1.next = cx.self;
                                    cx.prev = bx1.self;
                                    links[cx.self] = cx;
                                    links[bx1.self] = bx1;

                                    if(cx.self != c.self) {
                                        // move back
                                        bx0 = cx;
                                        cx = links[cx.next];
                                        continue;
                                    } else {
                                        // move forward
                                        bx0 = bx1;
                                        bx1 = links[bx0.prev];
                                        break;
                                    }
                                case IntTriangle.Orientation.counterClockWise:
                                    cx = bx0;
                                    bx0 = bx1;
                                    bx1 = links[bx1.prev];
                                    break;
                            }
                            bx1Bit = bx1.vertex.point.BitPack;
                        } while(bx1Bit < aBit0);
                    }

                    c = links[c.self];
                    a0 = links[c.next];
                    b0 = links[c.prev];


                    aBit0 = a0.vertex.point.BitPack;
                    bBit0 = b0.vertex.point.BitPack;

                    if(IntTriangle.IsNotLine(c.vertex.point, a0.vertex.point, b0.vertex.point)) {
                        triangles[counter++] = c.vertex.index;
                        triangles[counter++] = a0.vertex.index;
                        triangles[counter++] = b0.vertex.index;
                    }
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

                } //while
            }
        }
    }

}