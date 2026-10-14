using System;
using System.Collections.Generic;
using ClipperLib;
using iShape.Geometry;
using UnityEngine;

namespace SpriteAssist
{
    // Preserve the filled region of saved outlines while splitting intersecting or overlapping paths into simple outlines.
    // Do not approximate the shape beyond rounding to the same integer grid as iShape.
    internal static class ProvidedOutline
    {
        public static Vector2[][] Normalize(Vector2[][] paths, IntGeom geom, bool nonZero)
        {
            if (paths == null) throw new ArgumentNullException(nameof(paths));
            var clipper = new Clipper { StrictlySimple = true };
            foreach (var path in paths)
            {
                if (path == null)
                    throw new ArgumentException("Provided outline contains a null path.");
                var points = new List<IntPoint>(path.Length);
                foreach (var point in path)
                {
                    if (float.IsNaN(point.x) || float.IsNaN(point.y) ||
                        float.IsInfinity(point.x) || float.IsInfinity(point.y) ||
                        Math.Abs(point.x) >= geom.MaxValue || Math.Abs(point.y) >= geom.MaxValue)
                        throw new ArgumentException("Provided outline has invalid coordinates.");
                    var value = geom.Int(point);
                    points.Add(new IntPoint(value.x, value.y));
                }
                if (path.Length < 3) continue;
                // Paths that were already a point or line have no filled region; distinguish them from faces collapsed by rounding.
                if (!clipper.AddPath(points, PolyType.ptSubject, true) && !IsCollinear(path))
                    throw new ArgumentException("Provided outline collapses on the iShape integer grid.");
            }
            var solution = new List<List<IntPoint>>();
            var fill = nonZero ? PolyFillType.pftNonZero : PolyFillType.pftEvenOdd;
            if (!clipper.Execute(ClipType.ctUnion, solution, fill, fill))
                throw new InvalidOperationException("Unable to normalize provided outline.");
            var result = new Vector2[solution.Count][];
            for (int i = 0; i < solution.Count; i++)
            {
                result[i] = new Vector2[solution[i].Count];
                for (int j = 0; j < solution[i].Count; j++)
                    result[i][j] = geom.Float(new IntVector(solution[i][j].X, solution[i][j].Y));
            }
            return result;
        }

        private static bool IsCollinear(Vector2[] path)
        {
            var a = path[0];
            int next = 1;
            while (next < path.Length && path[next].Equals(a)) next++;
            if (next == path.Length) return true;
            double dx = (double)path[next].x - a.x;
            double dy = (double)path[next].y - a.y;
            foreach (var point in path)
                if (dx * ((double)point.y - a.y) != dy * ((double)point.x - a.x)) return false;
            return true;
        }
    }
}
