using System.Collections.Generic;
using UnityEngine;

namespace SpriteAssist
{
    internal readonly struct SelfIntersection
    {
        public readonly int pathIndex;
        public readonly int edgeA;
        public readonly int edgeB;
        public readonly int pointCount;
        public readonly float area;
        public readonly float minEdge;
        public readonly int minEdgeIndex;

        public SelfIntersection(int pathIndex, int edgeA, int edgeB, int pointCount, float area, float minEdge, int minEdgeIndex)
        {
            this.pathIndex = pathIndex;
            this.edgeA = edgeA;
            this.edgeB = edgeB;
            this.pointCount = pointCount;
            this.area = area;
            this.minEdge = minEdge;
            this.minEdgeIndex = minEdgeIndex;
        }
    }

    internal static class SelfIntersectionFinder
    {
        public static bool TryFind(IReadOnlyList<Vector2> points, out int edgeA, out int edgeB)
        {
            edgeA = -1;
            edgeB = -1;

            int count = points.Count;

            if (count < 4)
            {
                return false;
            }

            float[] xmin = new float[count];
            float[] xmax = new float[count];
            int[] order = new int[count];

            for (var i = 0; i < count; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % count];
                xmin[i] = Mathf.Min(a.x, b.x);
                xmax[i] = Mathf.Max(a.x, b.x);
                order[i] = i;
            }

            System.Array.Sort(order, (p, q) => xmin[p].CompareTo(xmin[q]));

            int bestA = int.MaxValue;
            int bestB = int.MaxValue;

            for (var oi = 0; oi < count; oi++)
            {
                int i = order[oi];
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % count];

                for (var oj = oi + 1; oj < count; oj++)
                {
                    int j = order[oj];

                    if (xmin[j] > xmax[i])
                    {
                        break;
                    }

                    int lo = Mathf.Min(i, j);
                    int hi = Mathf.Max(i, j);

                    if (hi - lo <= 1 || lo == 0 && hi == count - 1)
                    {
                        continue;
                    }

                    Vector2 c = points[j];
                    Vector2 d = points[(j + 1) % count];

                    if (TriangulationGeometry.SegmentsIntersect(a, b, c, d) && (lo < bestA || lo == bestA && hi < bestB))
                    {
                        bestA = lo;
                        bestB = hi;
                    }
                }
            }

            if (bestA == int.MaxValue)
            {
                return false;
            }

            edgeA = bestA;
            edgeB = bestB;
            return true;
        }

        public static bool TryFind(Vector2[][] paths, out SelfIntersection intersection)
        {
            for (var i = 0; i < paths.Length; i++)
            {
                if (TryFind(paths[i], i, out intersection))
                {
                    return true;
                }
            }

            intersection = default;
            return false;
        }

        private static bool TryFind(Vector2[] path, int pathIndex, out SelfIntersection intersection)
        {
            if (TryFind(path, out int edgeA, out int edgeB))
            {
                GetMinEdge(path, out float minEdge, out int minEdgeIndex);
                intersection = new SelfIntersection(pathIndex, edgeA, edgeB, path.Length, TriangulationGeometry.SignedArea(path), minEdge, minEdgeIndex);
                return true;
            }

            intersection = default;
            return false;
        }

        private static void GetMinEdge(Vector2[] path, out float minEdge, out int minEdgeIndex)
        {
            minEdge = float.MaxValue;
            minEdgeIndex = 0;

            for (var i = 0; i < path.Length; i++)
            {
                float edge = Vector2.Distance(path[i], path[(i + 1) % path.Length]);

                if (edge < minEdge)
                {
                    minEdge = edge;
                    minEdgeIndex = i;
                }
            }
        }
    }
}
