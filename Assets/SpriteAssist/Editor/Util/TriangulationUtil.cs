using LibTessDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpriteAssist
{
    public class TriangulationUtil
    {
        public static void Triangulate(Vector2[][] paths, float edgeSmoothing, bool nonzero, out Vector2[] vertices, out ushort[] triangles)
        {
            Tess tess = new Tess();

            foreach (Vector2[] path in paths)
            {
                List<ContourVertex> contour = new List<ContourVertex>();

                for (var i = 0; i < path.Length; i++)
                {
                    Vector2 oldPos = path[(path.Length + i - 1) % path.Length];
                    Vector2 currentPos = path[i];
                    Vector2 nextPos = path[(i + 1) % path.Length];

                    //edge smoothing
                    if (Vector2.Dot((currentPos - oldPos).normalized, (nextPos - oldPos).normalized) >= 0.99f + Mathf.Pow(edgeSmoothing, 3) * 0.01)
                    {
                        continue;
                    }

                    contour.Add(new ContourVertex(new Vec3(currentPos.x, currentPos.y, 0)));
                }

                tess.AddContour(contour, ContourOrientation.CounterClockwise);
            }

            WindingRule windingRule = nonzero ? WindingRule.NonZero : WindingRule.EvenOdd;
            tess.Tessellate(windingRule);
            vertices = (tess.Vertices ?? Array.Empty<ContourVertex>()).Select(v => new Vector2(v.Position.X, v.Position.Y)).ToArray();
            triangles = (tess.Elements ?? Array.Empty<int>()).Select(t => (ushort)t).ToArray();
        }

        public static void ExpandMeshThickness(ref Vector3[] v, ref int[] t, float thickness)
        {
            Vector3[] v2 = new Vector3[v.Length * 2];
            int[] t2 = new int[t.Length * 2];
            float halfThickness = thickness * 0.5f;

            for (var i = 0; i < v.Length; i++)
            {
                v2[i + v.Length * 0] = new Vector3(v[i].x, v[i].y, -halfThickness);
                v2[i + v.Length * 1] = new Vector3(v[i].x, v[i].y, halfThickness);
            }

            for (var i = 0; i < t.Length; i++)
            {
                //front
                t2[i + 0] = t[i + 0];

                //back(reversed)
                t2[t.Length * 2 - 1 - i] = t[i] + v.Length;
            }

            var edges = GetTriangulationEdges(t);
            List<int> sideTriangles = new List<int>();

            foreach (var edge in edges)
            {
                sideTriangles.Add(edge.index2);
                sideTriangles.Add(edge.index1);
                sideTriangles.Add(edge.index1 + v.Length);

                sideTriangles.Add(edge.index1 + v.Length);
                sideTriangles.Add(edge.index2 + v.Length);
                sideTriangles.Add(edge.index2);
            }

            var temp = t2.ToList();
            temp.AddRange(sideTriangles);

            v = v2;
            t = temp.ToArray();
        }

        private static Edge[] GetTriangulationEdges(int[] triangles)
        {
            HashSet<Edge> edges = new HashSet<Edge>();

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int t1 = triangles[i];
                int t2 = triangles[i + 1];
                int t3 = triangles[i + 2];

                AddOrRemoveEdge(edges, new Edge(t1, t2));
                AddOrRemoveEdge(edges, new Edge(t2, t3));
                AddOrRemoveEdge(edges, new Edge(t3, t1));
            }

            return edges.ToArray();
        }

        private static void AddOrRemoveEdge(HashSet<Edge> edges, Edge edge)
        {
            if (edges.Contains(edge))
            {
                edges.Remove(edge);
            }
            else if (edges.Contains(edge.GetSwapped()))
            {
                edges.Remove(edge.GetSwapped());
            }
            else
            {
                edges.Add(edge);
            }
        }

        private struct Edge
        {
            public int index1;
            public int index2;

            public Edge(int i1, int i2)
            {
                this.index1 = i1;
                this.index2 = i2;
            }

            public Edge GetSwapped()
            {
                return new Edge(index2, index1);
            }
        }
    }
}
