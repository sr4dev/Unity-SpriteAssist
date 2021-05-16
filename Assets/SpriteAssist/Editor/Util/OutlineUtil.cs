using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SpriteAssist
{
    public static class OutlineUtil
    {
        private static readonly MethodInfo _generateOutlineMethodInfo = typeof(UnityEditor.Sprites.SpriteUtility).GetMethod("GenerateOutlineFromSprite", BindingFlags.NonPublic | BindingFlags.Static);
        private const float FLOAT_TO_INT_SCALE = 1000f;
        private const float INT_TO_FLOAT_SCALE = 1 / FLOAT_TO_INT_SCALE;

        public static Vector2[][] GenerateOutline(Sprite sprite, SpriteConfigData data, MeshRenderType meshRenderType)
        {
            switch (meshRenderType)
            {
                case MeshRenderType.Transparent:
                    return GenerateTransparentOutline(sprite, data.transparentDetail, data.transparentAlphaTolerance, data.detectHoles);

                case MeshRenderType.Opaque:
                    return GenerateOpaqueOutline(sprite, data.opaqueDetail, data.opaqueAlphaTolerance);

                case MeshRenderType.SeparatedTransparent:
                    return GenerateSeparatedTransparent(sprite, data);

                default:
                    return Array.Empty<Vector2[]>();
            }
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
            Vector2[][] paths = GenerateTransparentOutline(sprite, 0.5f + detail * 0.5f, alphaTolerance, true);
            List<List<IntPoint>> intPointList = ConvertToIntPointList(paths, detail);
            List<List<IntPoint>> offsetIntPointList = new List<List<IntPoint>>();
            ClipperOffset offset = new ClipperOffset();
            offset.AddPaths(intPointList, JoinType.jtMiter, EndType.etClosedPolygon);
            offset.Execute(ref offsetIntPointList, -32);//TODO: magic number!
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
                    Vector2 point = simplifiedPath[j] * FLOAT_TO_INT_SCALE;
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
                    Vector2 point = path[j] * FLOAT_TO_INT_SCALE;
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
                    points[j] = new Vector2(intPoint.X, intPoint.Y) * INT_TO_FLOAT_SCALE;
                }

                outPaths[i] = points;
            }

            return outPaths;
        }
    }
}
