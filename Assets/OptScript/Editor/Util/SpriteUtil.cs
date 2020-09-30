using ClipperLib;
using LibTessDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace OptSprite
{
    public enum MeshRenderType
    {
        Transparent,
        Opaque,
        SeparatedTransparent
    }

    public static class SpriteUtil
    {
        private static readonly MethodInfo _generateOutlineMethodInfo = typeof(UnityEditor.Sprites.SpriteUtility).GetMethod("GenerateOutlineFromSprite", BindingFlags.NonPublic | BindingFlags.Static);

        public static float GetMinRectScale(Rect rect, Rect sRect)
        {
            return Mathf.Min(rect.width / sRect.width, rect.height / sRect.height);
        }

        public static void GetScaledVertices(Vector2[] vertices, float pixelsPerUnit, Vector2 pivot, Vector2 size, float additionalScale, bool isFlipY, bool clamp)
        {
            float scaledPixelsPerUnit = pixelsPerUnit * additionalScale;
            Vector2 scaledPivot = pivot * additionalScale;
            Vector2 scaledSize = size * additionalScale;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector2 vertex = vertices[i] * scaledPixelsPerUnit + scaledPivot;

                if (isFlipY)
                {
                    vertex.y = (vertex.y - scaledSize.y) * -1.0f;
                }

                if (clamp)
                {
                    vertex.x = Mathf.Clamp(vertex.x, 0, size.x);
                    vertex.y = Mathf.Clamp(vertex.y, 0, size.y);
                }

                vertices[i] = vertex;
            }
        }

        public static void GetMeshData(Sprite sprite, SpriteConfigData data, out Vector2[] vertices, out ushort[] triangles, MeshRenderType meshRenderType)
        {
            if (!TryGetMeshData(sprite, data, out vertices, out triangles, meshRenderType))
            {
                //fallback
                vertices = sprite.vertices;
                triangles = sprite.triangles;
            }

            Debug.Log(vertices.Length + ", " + triangles.Length);
        }

        private static bool TryGetMeshData(Sprite sprite, SpriteConfigData data, out Vector2[] vertices, out ushort[] triangles, MeshRenderType meshRenderType)
        {
            vertices = null;
            triangles = null;

            if (data != null && data.overriden)
            {
                Vector2[][] paths = GenerateOutline(sprite, data, meshRenderType);
                //TriangulateByPoly2Tri(paths, out vertices, out triangles);
                Triangulate(paths, data, out vertices, out triangles, meshRenderType);

                //validate
                if (vertices.Length < ushort.MaxValue)
                {
                    return true;
                }

                Debug.LogErrorFormat($"Too many veretics! Sprite {sprite.name} has {vertices.Length} vertices.");
            }

            return false;
        }

        public static void UpdateMesh(Sprite sprite, SpriteConfigData data, ref Mesh mesh, MeshRenderType meshRenderType)
        {
            GetMeshData(sprite, data, out var v, out var t, meshRenderType);

            Vector2[] uv = new Vector2[v.Length];

            for (var i = 0; i < uv.Length; i++)
            {
                uv[i] = v[i] * sprite.pixelsPerUnit + sprite.pivot;
                uv[i].x /= sprite.texture.width;
                uv[i].y /= sprite.texture.height;
            }

            mesh.Clear();
            mesh.SetVertices(Array.ConvertAll(v, i => (Vector3)i).ToList());
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(Array.ConvertAll(t, i => (int)i), 0);
        }

        private static Vector2[][] GenerateOutline(Sprite sprite, SpriteConfigData data, MeshRenderType meshRenderType)
        {
            switch (meshRenderType)
            {
                case MeshRenderType.Transparent:
                    return GenerateTransparentOutline(sprite, data.transparentDetail, data.transparentAlphaTolerance, data.detectHoles);
                    
                case MeshRenderType.Opaque:
                    return GenerateOpaqueOutline(sprite, data.opaqueDetail, data.opaqueAlphaTolerance);
                    
                case MeshRenderType.SeparatedTransparent:
                    return GenerateSeparatedTransparent(sprite, data);
            }

            return new Vector2[0][];
        }

        private static List<List<IntPoint>> ConvertToIntPointList(Vector2[][] paths, float simplify)
        {
            simplify = Mathf.Clamp01(1 - (simplify * 0.01f + 0.99f));
            List<List<IntPoint>> intPointPaths = new List<List<IntPoint>>(paths.Length);

            for (int i = 0; i < paths.Length; i++)
            {
                List<Vector2> simplifiedPath = new List<Vector2>(paths.Length);
                LineUtility.Simplify(paths[i].ToList(), simplify, simplifiedPath);
                List<IntPoint> intPointPath = new List<IntPoint>(simplifiedPath.Count);

                for (int j = 0; j < simplifiedPath.Count; j++)
                {
                    Vector2 point = simplifiedPath[j] * 1000;
                    intPointPath.Add(new IntPoint(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y)));
                }

                intPointPaths.Add(intPointPath);
            }

            return intPointPaths;
        }

        private static List<List<IntPoint>> ConvertToIntPointList(Vector2[][] paths)
        {
            List<List<IntPoint>> intPointPaths = new List<List<IntPoint>>(paths.Length);

            for (int i = 0; i < paths.Length; i++)
            {
                Vector2[] path = paths[i];
                List<IntPoint> intPointPath = new List<IntPoint>(path.Length);

                for (int j = 0; j < path.Length; j++)
                {
                    Vector2 point = path[j] * 1000;
                    intPointPath.Add(new IntPoint(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y)));
                }

                intPointPaths.Add(intPointPath);
            }

            return intPointPaths;
        }


        private static Vector2[][] ConvertToVector2Array(List<List<IntPoint>> intPointpaths)
        {
            Vector2[][] outPaths = new Vector2[intPointpaths.Count][];

            for (int i = 0; i < intPointpaths.Count; i++)
            {
                List<IntPoint> intPointPath = intPointpaths[i];
                Vector2[] points = new Vector2[intPointPath.Count];

                for (int j = 0; j < intPointPath.Count; j++)
                {
                    IntPoint intPoint = intPointPath[j];
                    points[j] = new Vector2(intPoint.X, intPoint.Y) * 0.001f;
                }

                outPaths[i] = points;
            }

            return outPaths;
        }

        private static void Triangulate(Vector2[][] paths, SpriteConfigData data, out Vector2[] vertices, out ushort[] triangles, MeshRenderType meshRenderType)
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

                    //edgw smoothing
                    var s = meshRenderType == MeshRenderType.Transparent ? data.transparentEdgeSmoothing : data.opaqueEdgeSmoothing;
                    if (Vector2.Dot((currentPos - oldPos).normalized, (nextPos - oldPos).normalized) >= 0.99f + Mathf.Pow(s, 3) * 0.01)
                    {
                        continue;
                    }

                    contour.Add(new ContourVertex(new Vec3(currentPos.x, currentPos.y, 0)));
                }

                tess.AddContour(contour, ContourOrientation.CounterClockwise);
            }

            tess.Tessellate(data.windingRule);
            vertices = tess.Vertices.Select(v => new Vector2(v.Position.X, v.Position.Y)).ToArray();
            triangles = tess.Elements.Select(t => (ushort)t).ToArray();
        }

        private static Vector2[][] GenerateTransparentOutline(Sprite sprite, float detail, byte alphaTolerance, bool detectHoles)
        {
            detail = Mathf.Pow(detail, 3);
            object[] parameters = new object[] { sprite, detail, alphaTolerance, detectHoles, null };
            _generateOutlineMethodInfo.Invoke(null, parameters);
            return (Vector2[][])parameters[4];
        }

        private static Vector2[][] GenerateOpaqueOutline(Sprite sprite, float detail, byte alphaTolerance)
        {
            Vector2[][] paths = GenerateTransparentOutline(sprite, 1, alphaTolerance, true);
            List<List<IntPoint>> intPointList = ConvertToIntPointList(paths, detail);
            List<List<IntPoint>> offsetIntPointList = new List<List<IntPoint>>();
            ClipperOffset offset = new ClipperOffset();
            offset.AddPaths(intPointList, JoinType.jtMiter, EndType.etClosedPolygon);
            offset.Execute(ref offsetIntPointList, -32);
            return ConvertToVector2Array(offsetIntPointList);
        }

        private static Vector2[][] GenerateSeparatedTransparent(Sprite sprite, SpriteConfigData data)
        {
            Vector2[][] transparentPaths = GenerateTransparentOutline(sprite, data.transparentDetail, data.transparentAlphaTolerance, data.detectHoles);
            Vector2[][] opaquePaths = GenerateOpaqueOutline(sprite, data.opaqueDetail, data.opaqueAlphaTolerance);
            List<List<IntPoint>> convertedTransparentPaths = ConvertToIntPointList(transparentPaths);
            List<List<IntPoint>> convertedOpaquePaths = ConvertToIntPointList(opaquePaths);
            List<List<IntPoint>> intersectionPaths = new List<List<IntPoint>>();
            Clipper clipper = new Clipper();
            clipper.AddPaths(convertedTransparentPaths, PolyType.ptSubject, true);
            clipper.AddPaths(convertedOpaquePaths, PolyType.ptClip, true);
            clipper.Execute(ClipType.ctDifference, intersectionPaths, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);
            return ConvertToVector2Array(intersectionPaths);
        }

    }
}
