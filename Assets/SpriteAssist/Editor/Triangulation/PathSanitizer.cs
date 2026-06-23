using System.Collections.Generic;
using iShape.Geometry;
using UnityEngine;

namespace SpriteAssist
{
    internal static class PathSanitizer
    {
        private const int MaxLocalRepairIterations = 8;
        private const int MaxLocalSelfIntersectionSpan = 8;

        public static Vector2[][] Sanitize(Vector2[][] paths, IntGeom geom)
        {
            List<Vector2[]> sanitizedPaths = new List<Vector2[]>();

            for (var i = 0; i < paths.Length; i++)
            {
                Vector2[] sanitizedPath = SanitizePath(paths[i], geom);

                if (sanitizedPath.Length >= 3 && !Mathf.Approximately(TriangulationGeometry.SignedArea(sanitizedPath), 0))
                {
                    sanitizedPaths.Add(sanitizedPath);
                }
            }

            return sanitizedPaths.ToArray();
        }

        public static bool TrySimplifyShortEdges(Vector2[][] paths, IntGeom geom, int minEdgeLength, out Vector2[][] simplifiedPaths)
        {
            List<Vector2[]> results = new List<Vector2[]>();
            bool changed = false;

            for (var i = 0; i < paths.Length; i++)
            {
                List<IntVector> points = new List<IntVector>();

                foreach (Vector2 point in paths[i])
                {
                    points.Add(geom.Int(point));
                }

                changed |= RemoveShortEdges(points, minEdgeLength);
                RemoveCollinearPoints(points);
                RepairLocalSelfIntersections(points, geom);
                RemoveDuplicatePoints(points);
                RemoveCollinearPoints(points);

                if (points.Count < 3)
                {
                    changed = true;
                    continue;
                }

                Vector2[] result = new Vector2[points.Count];

                for (var j = 0; j < points.Count; j++)
                {
                    result[j] = geom.Float(points[j]);
                }

                if (Mathf.Approximately(TriangulationGeometry.SignedArea(result), 0))
                {
                    changed = true;
                    continue;
                }

                results.Add(result);
            }

            simplifiedPaths = results.ToArray();
            return changed && simplifiedPaths.Length > 0;
        }

        public static int CountPoints(Vector2[][] paths)
        {
            int count = 0;

            foreach (Vector2[] path in paths)
            {
                count += path?.Length ?? 0;
            }

            return count;
        }

        private static Vector2[] SanitizePath(Vector2[] path, IntGeom geom)
        {
            if (path == null || path.Length < 3)
            {
                return System.Array.Empty<Vector2>();
            }

            List<IntVector> points = new List<IntVector>();

            foreach (Vector2 point in path)
            {
                IntVector intPoint = geom.Int(point);

                if (points.Count == 0 || points[points.Count - 1] != intPoint)
                {
                    points.Add(intPoint);
                }
            }

            if (points.Count > 1 && points[0] == points[points.Count - 1])
            {
                points.RemoveAt(points.Count - 1);
            }

            RemoveDuplicatePoints(points);
            RemoveCollinearPoints(points);
            RepairLocalSelfIntersections(points, geom);
            RemoveDuplicatePoints(points);
            RemoveCollinearPoints(points);

            Vector2[] sanitizedPath = new Vector2[points.Count];

            for (var i = 0; i < points.Count; i++)
            {
                sanitizedPath[i] = geom.Float(points[i]);
            }

            return sanitizedPath;
        }

        private static bool RemoveShortEdges(List<IntVector> points, int minLength)
        {
            bool removedAny = false;
            bool removed;
            long minSqrLength = (long)minLength * minLength;

            do
            {
                removed = false;

                for (var i = points.Count - 1; i >= 0 && points.Count >= 3; i--)
                {
                    IntVector previous = points[(i - 1 + points.Count) % points.Count];
                    IntVector current = points[i];
                    IntVector next = points[(i + 1) % points.Count];

                    if (SqrDistance(previous, current) < minSqrLength || SqrDistance(current, next) < minSqrLength)
                    {
                        points.RemoveAt(i);
                        removed = true;
                        removedAny = true;
                    }
                }
            }
            while (removed && points.Count >= 3);

            return removedAny;
        }

        private static long SqrDistance(IntVector a, IntVector b)
        {
            long dx = a.x - b.x;
            long dy = a.y - b.y;
            return dx * dx + dy * dy;
        }

        private static void RemoveDuplicatePoints(List<IntVector> points)
        {
            HashSet<IntVector> used = new HashSet<IntVector>();

            for (var i = 0; i < points.Count; i++)
            {
                if (!used.Add(points[i]))
                {
                    points.RemoveAt(i);
                    i--;
                }
            }
        }

        private static void RemoveCollinearPoints(List<IntVector> points)
        {
            bool removed;

            do
            {
                removed = false;

                for (var i = points.Count - 1; i >= 0 && points.Count >= 3; i--)
                {
                    IntVector previous = points[(i - 1 + points.Count) % points.Count];
                    IntVector current = points[i];
                    IntVector next = points[(i + 1) % points.Count];

                    if ((current - previous).CrossProduct(next - current) == 0)
                    {
                        points.RemoveAt(i);
                        removed = true;
                    }
                }
            }
            while (removed && points.Count >= 3);
        }

        private static void RepairLocalSelfIntersections(List<IntVector> points, IntGeom geom)
        {
            for (var iteration = 0; iteration < MaxLocalRepairIterations && points.Count >= 3; iteration++)
            {
                Vector2[] floats = new Vector2[points.Count];

                for (var i = 0; i < points.Count; i++)
                {
                    floats[i] = geom.Float(points[i]);
                }

                if (!SelfIntersectionFinder.TryFind(floats, out int edgeA, out int edgeB))
                {
                    return;
                }

                Vector2 intersectionPoint = TriangulationGeometry.GetIntersection(
                    floats[edgeA],
                    floats[(edgeA + 1) % floats.Length],
                    floats[edgeB],
                    floats[(edgeB + 1) % floats.Length]);
                IntVector intersection = geom.Int(intersectionPoint);

                int span = edgeB - edgeA;
                int inverseSpan = points.Count - span;

                if (span <= MaxLocalSelfIntersectionSpan)
                {
                    ReplaceLocalLoop(points, edgeA, edgeB, intersection);
                    continue;
                }

                if (inverseSpan <= MaxLocalSelfIntersectionSpan)
                {
                    ReplaceWrappedLocalLoop(points, edgeA, edgeB, intersection);
                    continue;
                }

                return;
            }
        }

        private static void ReplaceLocalLoop(List<IntVector> points, int edgeA, int edgeB, IntVector intersection)
        {
            points.RemoveRange(edgeA + 1, edgeB - edgeA);
            InsertIfNeeded(points, edgeA + 1, intersection);
        }

        private static void ReplaceWrappedLocalLoop(List<IntVector> points, int edgeA, int edgeB, IntVector intersection)
        {
            int keepCount = edgeB - edgeA;
            List<IntVector> repaired = new List<IntVector>(keepCount + 1) { intersection };

            for (var i = edgeA + 1; i <= edgeB; i++)
            {
                repaired.Add(points[i]);
            }

            points.Clear();
            points.AddRange(repaired);
        }

        private static void InsertIfNeeded(List<IntVector> points, int index, IntVector point)
        {
            IntVector previous = points[(index - 1 + points.Count) % points.Count];
            IntVector next = points[index % points.Count];

            if (point != previous && point != next)
            {
                points.Insert(index, point);
            }
        }
    }
}
