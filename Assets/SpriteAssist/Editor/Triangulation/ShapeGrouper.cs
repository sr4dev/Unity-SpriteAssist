using System.Collections.Generic;
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

            for (var i = 0; i < paths.Length; i++)
            {
                parents[i] = FindSmallestContainer(paths, i);
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

        private static int FindSmallestContainer(Vector2[][] paths, int pathIndex)
        {
            Vector2 point = paths[pathIndex][0];
            float currentArea = Mathf.Abs(TriangulationGeometry.SignedArea(paths[pathIndex]));
            int parent = -1;
            float parentArea = float.MaxValue;

            for (var i = 0; i < paths.Length; i++)
            {
                if (i == pathIndex || !TriangulationGeometry.ContainsPoint(paths[i], point))
                {
                    continue;
                }

                float area = Mathf.Abs(TriangulationGeometry.SignedArea(paths[i]));

                if (area <= currentArea)
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
