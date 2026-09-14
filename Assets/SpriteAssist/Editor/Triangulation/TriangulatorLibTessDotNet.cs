using LibTessDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpriteAssist
{
    internal sealed class TriangulatorLibTessDotNet : ITriangulator
    {
        public string DisplayName => "Stable: LibTessDotNet";

        public string Description =>
            "Robust and fast for any outline, no fallback.\n" +
            "Tends to produce thin sliver triangles, which rasterize less efficiently on the GPU (wasted pixel quads, edge overdraw).";

        public bool TryTriangulate(
            SpriteConfigData config,
            Vector2[][] paths,
            out Vector2[] vertices,
            out ushort[] triangles)
        {
            Vector2[][] smoothedPaths = PathSanitizer.ApplyEdgeSmoothing(paths, config.edgeSmoothing);
            return TriangulatePaths(smoothedPaths, config.useNonZero, ContourOrientation.CounterClockwise, out vertices, out triangles);
        }

        public bool TryTriangulateProvided(SpriteConfigData config, Vector2[][] paths,
            out Vector2[] vertices, out ushort[] triangles)
        {
            // 明示的に LibTess を選んだ場合も、保存輪郭の座標・向きを保持する。
            return TriangulatePaths(paths, config.useNonZero, ContourOrientation.Original, out vertices, out triangles);
        }

        private static bool TriangulatePaths(Vector2[][] paths, bool nonzero, ContourOrientation orientation,
            out Vector2[] vertices, out ushort[] triangles)
        {
            Tess tess = new Tess();

            foreach (Vector2[] path in paths)
            {
                List<ContourVertex> contour = new List<ContourVertex>(path.Length);

                for (var i = 0; i < path.Length; i++)
                {
                    Vector2 currentPos = path[i];
                    contour.Add(new ContourVertex(new Vec3(currentPos.x, currentPos.y, 0)));
                }

                tess.AddContour(contour, orientation);
            }

            WindingRule windingRule = nonzero ? WindingRule.NonZero : WindingRule.EvenOdd;
            tess.Tessellate(windingRule);
            vertices = (tess.Vertices ?? Array.Empty<ContourVertex>()).Select(v => new Vector2(v.Position.X, v.Position.Y)).ToArray();
            triangles = (tess.Elements ?? Array.Empty<int>()).Select(t => (ushort)t).ToArray();

            return true;
        }
    }
}
