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
            float edgeSmoothing = config.edgeSmoothing;
            bool nonzero = config.useNonZero;

            Tess tess = new Tess();

            foreach (Vector2[] path in paths)
            {
                List<ContourVertex> contour = new List<ContourVertex>();

                for (var i = 0; i < path.Length; i++)
                {
                    Vector2 oldPos = path[(path.Length + i - 1) % path.Length];
                    Vector2 currentPos = path[i];
                    Vector2 nextPos = path[(i + 1) % path.Length];

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

            return true;
        }
    }
}
