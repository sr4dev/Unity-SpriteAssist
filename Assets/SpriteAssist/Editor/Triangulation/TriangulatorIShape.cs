using iShape.Geometry;
using iShape.Geometry.Container;
using iShape.Triangulation.Shape.Delaunay;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace SpriteAssist
{
    internal sealed class TriangulatorIShape : ITriangulator
    {
        private const int DirectTriangulationPointLimit = 1000;
        private static readonly int[] RetryMinEdgeLengths = { 150, 250, 400, 600 };

        public string DisplayName => "Quality: iShapeTriangulation";

        public string Description =>
            "Well-shaped (Delaunay) triangles with fewer slivers.\n" +
            "May fall back to LibTessDotNet on some outlines, and can briefly freeze the Editor on complex meshes.";

        public bool TryTriangulate(
            SpriteConfigData config,
            Vector2[][] paths,
            out Vector2[] vertices,
            out ushort[] triangles)
        {
            vertices = Array.Empty<Vector2>();
            triangles = Array.Empty<ushort>();

            if (paths == null || paths.Length == 0)
            {
                return false;
            }

            IntGeom geom = IntGeom.DefGeom;
            Vector2[][] smoothedPaths = PathSanitizer.ApplyEdgeSmoothing(paths, config.edgeSmoothing);
            Vector2[][] sanitizedPaths = PathSanitizer.Sanitize(smoothedPaths, geom);
            int sanitizedPointCount = PathSanitizer.CountPoints(sanitizedPaths);

            if (sanitizedPaths.Length == 0)
            {
                Debug.LogWarning($"iShape triangulation skipped. No valid paths after sanitize. inputPaths={paths.Length}, inputPoints={PathSanitizer.CountPoints(paths)}");
                return false;
            }

            if (SelfIntersectionFinder.TryFind(sanitizedPaths, out SelfIntersection intersection))
            {
                if (TryRepairSelfIntersections(sanitizedPaths, geom, config.useNonZero, out Vector2[][] repairedPaths))
                {
                    sanitizedPaths = repairedPaths;
                    sanitizedPointCount = PathSanitizer.CountPoints(sanitizedPaths);
                }

                if (SelfIntersectionFinder.TryFind(sanitizedPaths, out intersection))
                {
                    if (TryTriangulateSimplified(sanitizedPaths, geom, sanitizedPointCount, config.useNonZero, out vertices, out triangles, out _))
                    {
                        return true;
                    }

                    Debug.LogWarning($"iShape triangulation skipped. Self-intersection remains after sanitize. inputPaths={paths.Length}, inputPoints={PathSanitizer.CountPoints(paths)}, sanitizedPaths={sanitizedPaths.Length}, sanitizedPoints={sanitizedPointCount}, path={intersection.pathIndex}, edges={intersection.edgeA}-{(intersection.edgeA + 1) % intersection.pointCount}/{intersection.edgeB}-{(intersection.edgeB + 1) % intersection.pointCount}, points={intersection.pointCount}, area={intersection.area:0.###}, minEdge={intersection.minEdge:0.######}@{intersection.minEdgeIndex}");
                    return false;
                }
            }

            if (sanitizedPointCount <= DirectTriangulationPointLimit)
            {
                return TryTriangulateDirect(sanitizedPaths, geom, sanitizedPointCount, config.useNonZero, out vertices, out triangles);
            }

            return TryTriangulateLarge(sanitizedPaths, geom, sanitizedPointCount, config.useNonZero, out vertices, out triangles);
        }

        private static bool TryTriangulateDirect(
            Vector2[][] sanitizedPaths,
            IntGeom geom,
            int sanitizedPointCount,
            bool useNonZero,
            out Vector2[] vertices,
            out ushort[] triangles)
        {
            try
            {
                return TriangulateGroups(sanitizedPaths, geom, out vertices, out triangles);
            }
            catch (Exception firstException)
            {
                if (TryTriangulateSimplified(sanitizedPaths, geom, sanitizedPointCount, useNonZero, out vertices, out triangles, out Exception retryException))
                {
                    return true;
                }

                LogRetryException(retryException);
                Debug.LogWarning($"iShape triangulation failed. {firstException.GetType().Name}: {firstException.Message}\n{firstException.StackTrace}");
                vertices = Array.Empty<Vector2>();
                triangles = Array.Empty<ushort>();
                return false;
            }
        }

        private static bool TryTriangulateLarge(
            Vector2[][] sanitizedPaths,
            IntGeom geom,
            int sanitizedPointCount,
            bool useNonZero,
            out Vector2[] vertices,
            out ushort[] triangles)
        {
            if (TryTriangulateSimplified(sanitizedPaths, geom, sanitizedPointCount, useNonZero, out vertices, out triangles, out Exception retryException))
            {
                return true;
            }

            LogRetryException(retryException);
            vertices = Array.Empty<Vector2>();
            triangles = Array.Empty<ushort>();
            return false;
        }

        private static bool TryTriangulateSimplified(
            Vector2[][] sanitizedPaths,
            IntGeom geom,
            int pointCount,
            bool useNonZero,
            out Vector2[] vertices,
            out ushort[] triangles,
            out Exception retryException)
        {
            vertices = Array.Empty<Vector2>();
            triangles = Array.Empty<ushort>();
            retryException = null;

            for (int i = 0; i < RetryMinEdgeLengths.Length; i++)
            {
                int minEdgeLength = RetryMinEdgeLengths[i];

                if (!PathSanitizer.TrySimplifyShortEdges(sanitizedPaths, geom, minEdgeLength, out Vector2[][] simplifiedPaths))
                {
                    continue;
                }

                if (SelfIntersectionFinder.TryFind(simplifiedPaths, out _) &&
                    !TryRepairSelfIntersections(simplifiedPaths, geom, useNonZero, out simplifiedPaths))
                {
                    continue;
                }

                try
                {
                    if (TriangulateGroups(simplifiedPaths, geom, out vertices, out triangles))
                    {
                        return true;
                    }
                }
                catch (Exception e)
                {
                    retryException = e;
                }
            }

            return false;
        }

        private static bool TryRepairSelfIntersections(
            Vector2[][] paths,
            IntGeom geom,
            bool useNonZero,
            out Vector2[][] repairedPaths)
        {
            if (!PathSanitizer.TryRepairSelfIntersections(paths, geom, useNonZero, out repairedPaths))
            {
                return false;
            }

            repairedPaths = PathSanitizer.Sanitize(repairedPaths, geom);
            return repairedPaths.Length > 0 && !SelfIntersectionFinder.TryFind(repairedPaths, out _);
        }

        private static void LogRetryException(Exception retryException)
        {
            if (retryException != null)
            {
                Debug.LogWarning($"iShape triangulation failed after retry. {retryException.GetType().Name}: {retryException.Message}\n{retryException.StackTrace}");
            }
        }

        private static bool TriangulateGroups(Vector2[][] paths, IntGeom geom, out Vector2[] vertices, out ushort[] triangles)
        {
            ShapeGroup[] groups = ShapeGrouper.BuildGroups(paths);
            List<Vector2> vertexList = new List<Vector2>();
            List<ushort> triangleList = new List<ushort>();

            foreach (ShapeGroup group in groups)
            {
                Vector2[] hullPath = ShapeGrouper.NormalizeOrientation(paths[group.hull], true);
                IntVector[] hull = geom.Int(hullPath);
                IntVector[][] holes = new IntVector[group.holes.Count][];

                for (var i = 0; i < group.holes.Count; i++)
                {
                    Vector2[] holePath = ShapeGrouper.NormalizeOrientation(paths[group.holes[i]], false);
                    holes[i] = ShapeGrouper.ShouldUseHole(hullPath, holePath) ? geom.Int(holePath) : Array.Empty<IntVector>();
                }

                holes = RemoveEmptyHoles(holes);

                IntShape shape = new IntShape(hull, holes);
                PlainShape plainShape = new PlainShape(shape, Allocator.Temp);
                NativeArray<int> nativeTriangles = default;

                try
                {
                    nativeTriangles = plainShape.DelaunayTriangulate(Allocator.Temp);
                    int vertexOffset = vertexList.Count;

                    for (var i = 0; i < plainShape.points.Length; i++)
                    {
                        vertexList.Add(geom.Float(plainShape.points[i]));
                    }

                    for (var i = 0; i < nativeTriangles.Length; i++)
                    {
                        int index = nativeTriangles[i] + vertexOffset;

                        if (index > ushort.MaxValue)
                        {
                            Debug.LogWarning($"iShape triangulation skipped. Vertex index exceeds ushort max. index={index}, vertices={plainShape.points.Length}, groups={groups.Length}");
                            vertices = Array.Empty<Vector2>();
                            triangles = Array.Empty<ushort>();
                            return false;
                        }

                        triangleList.Add((ushort)index);
                    }
                }
                finally
                {
                    if (nativeTriangles.IsCreated)
                    {
                        nativeTriangles.Dispose();
                    }

                    plainShape.Dispose();
                }
            }

            vertices = vertexList.ToArray();
            triangles = triangleList.ToArray();
            return true;
        }

        private static IntVector[][] RemoveEmptyHoles(IntVector[][] holes)
        {
            List<IntVector[]> result = new List<IntVector[]>();

            foreach (IntVector[] hole in holes)
            {
                if (hole.Length > 0)
                {
                    result.Add(hole);
                }
            }

            return result.ToArray();
        }
    }
}
