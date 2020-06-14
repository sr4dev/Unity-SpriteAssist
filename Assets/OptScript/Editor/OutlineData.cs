using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Poly2Tri;

namespace OptSprite
{
    public class OutlineData
    {
        public Vector2[] outside;
        public List<Vector2[]> holes;

        public OutlineData(Vector2[] baseOutside = null)
        {
            outside = baseOutside;
            holes = new List<Vector2[]>();
        }

        public void CreateMeshData(out List<Vector2> vertices, out List<ushort> triangles, ushort offset = 0)
        {
            Polygon poly = new Polygon(ConvertPoints(outside));

            foreach (Vector2[] hole in holes)
            {
                poly.AddHole(new Polygon(ConvertPoints(hole)));
            }

            DTSweepContext tcx = new DTSweepContext();
            tcx.PrepareTriangulation(poly);
            DTSweep.Triangulate(tcx);

            IList<DelaunayTriangle> delaunayTriangle = poly.Triangles;
            Dictionary<uint, ushort> codeToIndex = new Dictionary<uint, ushort>();
            List<Vector2> vertexList = new List<Vector2>();

            foreach (DelaunayTriangle t in delaunayTriangle)
            {
                foreach (var p in t.Points)
                {
                    if (codeToIndex.ContainsKey(p.VertexCode))
                    {
                        continue;
                    }

                    codeToIndex[p.VertexCode] = (ushort)vertexList.Count;
                    Vector2 pos = new Vector2(p.Xf, p.Yf);
                    vertexList.Add(pos);
                }
            }

            ushort[] indices = new ushort[delaunayTriangle.Count * 3];
            int indicesCount = 0;

            for (int i = 0; i < delaunayTriangle.Count; i++)
            {
                indices[indicesCount++] = (ushort)(codeToIndex[delaunayTriangle[i].Points[0].VertexCode] + offset);
                indices[indicesCount++] = (ushort)(codeToIndex[delaunayTriangle[i].Points[1].VertexCode] + offset);
                indices[indicesCount++] = (ushort)(codeToIndex[delaunayTriangle[i].Points[2].VertexCode] + offset);
            }

            vertices = vertexList;
            triangles = indices.ToList();
        }

        private static PolygonPoint[] ConvertPoints(Vector2[] points)
        {
            int count = points.Length;
            PolygonPoint[] result = new PolygonPoint[count];

            for (int i = 0; i < count; i++)
            {
                Vector2 p = points[i];
                result[i] = new PolygonPoint(p.x, p.y);
            }

            return result;
        }
    }

}
