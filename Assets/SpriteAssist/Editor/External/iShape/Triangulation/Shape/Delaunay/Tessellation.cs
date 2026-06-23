using System;
using iShape.Geometry;
using iShape.Geometry.Container;
using iShape.Geometry.Extension;
using Unity.Collections;
using UnityEngine;

namespace iShape.Triangulation.Shape.Delaunay {

    internal readonly struct Validator {
        private static readonly float mergeCos = Mathf.Cos(0.8f * Mathf.PI);
        internal static readonly float sqrMergeCos = mergeCos * mergeCos;
        private readonly float maxArea;
        private readonly IntGeom intGeom;

        internal Validator(IntGeom intGeom, float maxArea) {
            this.intGeom = intGeom;
            this.maxArea = 2f * maxArea;
        }

        internal int TestRegular(Triangle triangle) {
            var a = intGeom.Float(triangle.vA.point);
            var b = intGeom.Float(triangle.vB.point);
            var c = intGeom.Float(triangle.vC.point);

            var ab = a.SqrDistance(b);
            var ca = c.SqrDistance(a);
            var bc = b.SqrDistance(c);

            float s0 = a.x * (c.y - b.y) + b.x * (a.y - c.y) + c.x * (b.y - a.y);
            float s1;

            int k;
            float sCos;
            
            if (ab >= bc + ca) {
                // c, ab
                k = 2;
                float l = bc + ca - ab;
                sCos = l * l / (4 * bc * ca);
                s1 = s0 / (1 - sCos);
            } else if (bc >= ca + ab) {
                // a, bc
                k = 0;
                float l = ca + ab - bc;
                sCos = l * l / (4 * ca * ab);
                s1 = s0 / (1 - sCos);
            } else if (ca >= bc + ab) {
                // b, ca
                k = 1;
                float l = bc + ab - ca;
                sCos = l * l / (4 * bc * ab);
                s1 = s0 / (1 - sCos);
            } else {
                if (ab >= bc && ab >= ca) {
                    k = 2;
                } else if (bc >= ca) {
                    k = 0;
                } else {
                    k = 1;
                }

                s1 = s0;
            }

            if (s1 > maxArea) {
                return k;
            }

            return -1;
        }
        
        internal static float SqrCos(IntVector a, IntVector b, IntVector c) {
            long ab = a.SqrDistance(b);
            long ca = c.SqrDistance(a);
            long bc = b.SqrDistance(c);

            if (ab >= bc + ca) {
                float aa = bc;
                float bb = ca;
                float cc = ab;

                float l = aa + bb - cc;
                return l * l / (4 * aa * bb);
            }

            return 0f;
        }
    }

    public static class TessellationExtension {
        public static Delaunay Tessellate(ref this PlainShape self, Allocator allocator, IntGeom intGeom, float maxEdge, NativeArray<IntVector> extraPoints, float maxArea = 0) {
            long iEdge = intGeom.Int(maxEdge);
            var delaunay = self.Delaunay(iEdge, extraPoints, allocator);

            float area;
            if (maxArea > 0) {
                area = maxArea;
            } else {
                area = 0.4f * maxEdge * maxEdge;
            }
            delaunay.Tessellate(intGeom, area);
            return delaunay;
        }
        
        public static Delaunay Tessellate(ref this PlainShape self, Allocator allocator, IntGeom intGeom, float maxEdge, float maxArea = 0) {
            long iEdge = intGeom.Int(maxEdge);
            var extraPoints = new NativeArray<IntVector>(0, Allocator.Temp);
            var delaunay = self.Delaunay(iEdge, extraPoints, allocator);
            extraPoints.Dispose();

            float area;
            if (maxArea > 0) {
                area = maxArea;
            } else {
                area = 0.4f * maxEdge * maxEdge;
            }
            delaunay.Tessellate(intGeom, area);
            return delaunay;
        }

        public static void Tessellate(ref this Delaunay self, IntGeom intGeom, float maxArea) {
            var validator = new Validator(intGeom, maxArea);
            var unprocessed = new IndexBuffer(self.triangles.Count, Allocator.Temp);
            
            var fixIndices = new NativeArray<int>(4, Allocator.Temp);

            while (unprocessed.hasNext) {
                int i = unprocessed.Next();
                var triangle = self.triangles[i];

                int k = validator.TestRegular(triangle);

                if (k < 0) {
                    continue;
                }

                int nIx = triangle.Neighbor(k);

                if (nIx < 0) {
                    continue;
                }

                var p = triangle.CircumscribedCenter();

                var neighbor = self.triangles[nIx];

                if (!neighbor.IsContain(p)) {
                    continue;
                }

                int j = neighbor.Opposite(triangle.index);
                int j_next = (j + 1) % 3;
                int j_prev = (j + 2) % 3;

                if (neighbor.Neighbor(j_next) == -1 || neighbor.Neighbor(j_prev) == -1) {
                    var njp = neighbor.Vertex(j).point;
                    var nextCos = Validator.SqrCos(neighbor.Vertex(j_prev).point, njp, p);
                    if (nextCos > Validator.sqrMergeCos) {
                        continue;
                    }

                    var prevCos = Validator.SqrCos(njp, neighbor.Vertex(j_next).point, p);
                    if (prevCos > Validator.sqrMergeCos) {
                        continue;
                    }
                }
                
                int k_next = (k + 1) % 3;
                int k_prev = (k + 2) % 3;

                int l = neighbor.Opposite(i);

                int l_next = (l + 1) % 3;
                int l_prev = (l + 2) % 3;

                var vertex = new Vertex(self.points.Count, Vertex.Nature.extraTessellated, p);
                self.points.Add(p);

                int n = self.triangles.Count;

                var t0 = triangle;
                t0.SetVertex(k_prev, vertex);
                t0.SetNeighbor(k_next, n);
                self.triangles[i] = t0;
                unprocessed.Add(t0.index);


                var t1 = neighbor;
                t1.SetVertex(l_next, vertex);
                t1.SetNeighbor(l_prev, n + 1);
                self.triangles[nIx] = t1;
                unprocessed.Add(t1.index);

                
                var t2Neighbor = triangle.Neighbor(k_next);
                var t2 = new Triangle(
                    n,
                    triangle.Vertex(k),
                    vertex,
                    triangle.Vertex(k_prev),
                    n + 1,
                    t2Neighbor,
                    i
                );

                if (t2Neighbor >= 0) {
                    var t2n = self.triangles[t2Neighbor];
                    t2n.UpdateOpposite(i, n);
                    self.triangles[t2Neighbor] = t2n;
                }

                self.triangles.Add(t2);
                unprocessed.Add(t2.index);

                var t3Neighbor = neighbor.Neighbor(l_prev);
                var t3 = new Triangle(
                    n + 1,
                    neighbor.Vertex(l),
                    neighbor.Vertex(l_next),
                    vertex,
                    n,
                    nIx,
                    t3Neighbor
                );

                if (t3Neighbor >= 0) {
                    var t3n = self.triangles[t3Neighbor];
                    t3n.UpdateOpposite(nIx, n + 1);
                    self.triangles[t3Neighbor] = t3n;
                }

                self.triangles.Add(t3);
                unprocessed.Add(t3.index);

                fixIndices[0] = i;
                fixIndices[1] = nIx;
                fixIndices[2] = n;
                fixIndices[3] = n + 1;
                self.Fix(ref unprocessed, fixIndices);
            }
            
            unprocessed.Dispose();
            fixIndices.Dispose();
        }
    }


    internal static class TessellationExt {
        internal static IntVector CircumscribedCenter(this Triangle self) {
            var a = self.vA.point;
            var b = self.vB.point;
            var c = self.vC.point;
            double ax = a.x;
            double ay = a.y;
            double bx = b.x;
            double by = b.y;
            double cx = c.x;
            double cy = c.y;

            double d = 2 * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
            double aa = ax * ax + ay * ay;
            double bb = bx * bx + by * by;
            double cc = cx * cx + cy * cy;
            double x = (aa * (by - cy) + bb * (cy - ay) + cc * (ay - by)) / d;
            double y = (aa * (cx - bx) + bb * (ax - cx) + cc * (bx - ax)) / d;

            return new IntVector((long) Math.Round(x, MidpointRounding.AwayFromZero), (long) Math.Round(y, MidpointRounding.AwayFromZero));
        }

        internal static bool IsContain(this Triangle self, IntVector p) {
            var a = self.vA.point;
            var b = self.vB.point;
            var c = self.vC.point;

            var d1 = Sign(p, a, b);
            var d2 = Sign(p, b, c);
            var d3 = Sign(p, c, a);

            bool has_neg = d1 < 0 || d2 < 0 || d3 < 0;
            bool has_pos = d1 > 0 || d2 > 0 || d3 > 0;

            return !(has_neg && has_pos);
        }

        private static long Sign(IntVector a, IntVector b, IntVector c) {
            return (a.x - c.x) * (b.y - c.y) - (b.x - c.x) * (a.y - c.y);
        }
    }

}