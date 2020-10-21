using LibTessDotNet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpriteAssist
{
    public class TriangulationUtil
    {
        public static void Triangulate(Vector2[][] paths, float edgeSmoothing, WindingRule windingRule, out Vector2[] vertices, out ushort[] triangles)
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

            tess.Tessellate(windingRule);
            vertices = tess.Vertices.Select(v => new Vector2(v.Position.X, v.Position.Y)).ToArray();
            triangles = tess.Elements.Select(t => (ushort)t).ToArray();
        }
    }
}
