using System.Collections.Generic;
using ClipperLib;
using iShape.Geometry;
using UnityEngine;

namespace SpriteAssist
{
    internal struct ShapeGroup
    {
        public readonly int hull;
        public readonly List<int> holes;

        public ShapeGroup(int hull)
        {
            this.hull = hull;
            holes = new List<int>();
        }
    }

    internal static class ShapeGrouper
    {
        public static ShapeGroup[] BuildGroups(Vector2[][] paths)
        {
            List<ShapeGroup> groups = new List<ShapeGroup>();
            int[] parents = new int[paths.Length];
            int[] depths = new int[paths.Length];
            var integerPaths = new List<IntPoint>[paths.Length];
            var areas = new double[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                integerPaths[i] = new List<IntPoint>(paths[i].Length);
                foreach (var point in paths[i])
                {
                    var value = IntGeom.DefGeom.Int(point);
                    integerPaths[i].Add(new IntPoint(value.x, value.y));
                }
                areas[i] = System.Math.Abs(Clipper.Area(integerPaths[i]));
            }

            for (var i = 0; i < paths.Length; i++)
            {
                parents[i] = FindSmallestContainer(integerPaths, areas, i);
            }

            for (var i = 0; i < paths.Length; i++)
            {
                depths[i] = GetDepth(parents, i);

                if (depths[i] % 2 == 0)
                {
                    groups.Add(new ShapeGroup(i));
                }
            }

            for (var i = 0; i < paths.Length; i++)
            {
                if (depths[i] % 2 == 0)
                {
                    continue;
                }

                int hull = parents[i];
                ShapeGroup? group = FindGroup(groups, hull);

                if (group.HasValue)
                {
                    group.Value.holes.Add(i);
                }
            }

            return groups.ToArray();
        }

        public static Vector2[] NormalizeOrientation(Vector2[] path, bool clockwise)
        {
            bool isClockwise = TriangulationGeometry.SignedArea(path) < 0;

            if (isClockwise == clockwise)
            {
                return path;
            }

            Vector2[] reversed = new Vector2[path.Length];

            for (var i = 0; i < path.Length; i++)
            {
                reversed[i] = path[path.Length - 1 - i];
            }

            return reversed;
        }

        public static bool ShouldUseHole(Vector2[] hullPath, Vector2[] holePath)
        {
            GetMinPathDistance(hullPath, holePath, out float distance);
            CountPathIntersections(hullPath, holePath, out int crossings, out int touches);
            return distance > 0f && crossings == 0 && touches == 0;
        }

        private static int GetDepth(int[] parents, int index)
        {
            int depth = 0;
            int parent = parents[index];
            int guard = 0;

            while (parent >= 0 && guard++ < parents.Length)
            {
                depth++;
                parent = parents[parent];
            }

            if (guard > parents.Length)
            {
                return 0;
            }

            return depth;
        }

        private static ShapeGroup? FindGroup(List<ShapeGroup> groups, int hull)
        {
            foreach (ShapeGroup group in groups)
            {
                if (group.hull == hull)
                {
                    return group;
                }
            }

            return null;
        }

        private static int FindSmallestContainer(List<IntPoint>[] paths, double[] areas, int pathIndex)
        {
            double currentArea = areas[pathIndex];
            int parent = -1;
            double parentArea = double.MaxValue;

            for (var i = 0; i < paths.Length; i++)
            {
                if (i == pathIndex)
                {
                    continue;
                }

                double area = areas[i];

                if (area <= currentArea || area >= parentArea || !ContainsPath(paths[i], paths[pathIndex]))
                {
                    continue;
                }

                if (area < parentArea)
                {
                    parent = i;
                    parentArea = area;
                }
            }

            return parent;
        }

        private static bool ContainsPath(List<IntPoint> container, List<IntPoint> child)
        {
            // 先頭点が外周に接するだけの別の島を、穴と誤判定しない。
            // 正規化済みの輪郭について、境界上と内部を整数座標で区別する。
            bool hasInteriorPoint = false;
            for (int i = 0; i < child.Count; i++)
            {
                int location = Clipper.PointInPolygon(child[i], container);
                if (location == 0) return false;
                hasInteriorPoint |= location == 1;
                var next = child[(i + 1) % child.Count];
                var midpoint = new IntPoint((child[i].X + next.X) / 2, (child[i].Y + next.Y) / 2);
                location = Clipper.PointInPolygon(midpoint, container);
                if (location == 0) return false;
                hasInteriorPoint |= location == 1;
            }
            return hasInteriorPoint;
        }

        private static void GetMinPathDistance(Vector2[] pathA, Vector2[] pathB, out float minDistance)
        {
            minDistance = float.MaxValue;

            for (var i = 0; i < pathA.Length; i++)
            {
                Vector2 a0 = pathA[i];
                Vector2 a1 = pathA[(i + 1) % pathA.Length];

                for (var j = 0; j < pathB.Length; j++)
                {
                    Vector2 b0 = pathB[j];
                    Vector2 b1 = pathB[(j + 1) % pathB.Length];
                    float distance = TriangulationGeometry.SegmentDistance(a0, a1, b0, b1);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }
                }
            }
        }

        private static void CountPathIntersections(Vector2[] pathA, Vector2[] pathB, out int crossings, out int touches)
        {
            crossings = 0;
            touches = 0;

            for (var i = 0; i < pathA.Length; i++)
            {
                Vector2 a0 = pathA[i];
                Vector2 a1 = pathA[(i + 1) % pathA.Length];

                for (var j = 0; j < pathB.Length; j++)
                {
                    Vector2 b0 = pathB[j];
                    Vector2 b1 = pathB[(j + 1) % pathB.Length];

                    if (TriangulationGeometry.SegmentsIntersect(a0, a1, b0, b1))
                    {
                        crossings++;
                    }
                    else if (TriangulationGeometry.SegmentsTouch(a0, a1, b0, b1))
                    {
                        touches++;
                    }
                }
            }
        }
    }
}
