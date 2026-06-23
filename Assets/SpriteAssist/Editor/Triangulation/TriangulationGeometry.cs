using UnityEngine;

namespace SpriteAssist
{
    internal static class TriangulationGeometry
    {
        public static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        public static float SignedArea(Vector2[] polygon)
        {
            float area = 0;

            for (var i = 0; i < polygon.Length; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % polygon.Length];
                area += a.x * b.y - b.x * a.y;
            }

            return area * 0.5f;
        }

        public static bool SegmentsIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float abC = Cross(b - a, c - a);
            float abD = Cross(b - a, d - a);
            float cdA = Cross(d - c, a - c);
            float cdB = Cross(d - c, b - c);

            return abC * abD < 0 && cdA * cdB < 0;
        }

        public static bool SegmentsTouch(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            const float epsilon = 0.000001f;
            return PointOnSegment(a, b, c, epsilon) || PointOnSegment(a, b, d, epsilon) || PointOnSegment(c, d, a, epsilon) || PointOnSegment(c, d, b, epsilon);
        }

        public static float SegmentDistance(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            if (SegmentsIntersect(a, b, c, d) || SegmentsTouch(a, b, c, d))
            {
                return 0f;
            }

            return Mathf.Min(
                DistancePointSegment(a, c, d),
                Mathf.Min(
                    DistancePointSegment(b, c, d),
                    Mathf.Min(DistancePointSegment(c, a, b), DistancePointSegment(d, a, b))));
        }

        public static float DistancePointSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSquared = Vector2.Dot(ab, ab);

            if (Mathf.Approximately(lengthSquared, 0))
            {
                return Vector2.Distance(point, a);
            }

            float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSquared);
            return Vector2.Distance(point, a + ab * t);
        }

        public static Vector2 GetIntersection(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            Vector2 ab = b - a;
            Vector2 cd = d - c;
            float denominator = Cross(ab, cd);

            if (Mathf.Approximately(denominator, 0))
            {
                return a;
            }

            float t = Cross(c - a, cd) / denominator;
            return a + ab * t;
        }

        public static bool ContainsPoint(Vector2[] polygon, Vector2 point)
        {
            bool inside = false;

            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 pi = polygon[i];
                Vector2 pj = polygon[j];

                if ((pi.y > point.y) != (pj.y > point.y) && point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static bool PointOnSegment(Vector2 a, Vector2 b, Vector2 p, float epsilon)
        {
            float cross = Mathf.Abs(Cross(b - a, p - a));

            if (cross > epsilon)
            {
                return false;
            }

            float dot = Vector2.Dot(p - a, p - b);
            return dot <= epsilon;
        }
    }
}
