using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace OptSprite
{
    static class SpriteUtil
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

        public static void GetMeshData(Sprite sprite, OptSpriteData configData, out Vector2[] vertices, out ushort[] triangles)
        {
            if (configData == null || configData.overriden == false)
            {
                vertices = sprite.vertices;
                triangles = sprite.triangles;
                return;
            }

            List<OutlineData> outlineDataList = new List<OutlineData>();
            Vector2[][] paths = GenerateOutline(sprite, configData.detail, configData.alphaTolerance, configData.detectHoles);

            //TODO
            foreach (Vector2[] path in paths)
            {
                foreach (OutlineData outlineData in outlineDataList)
                {
                    if (PolyContainsPoly(outlineData.outside, path))
                    {
                        foreach (Vector2[] hole in outlineData.holes)
                        {
                            if (PolyContainsPoly(hole, path))
                            {
                                goto outlineDataNext;
                            }
                        }

                        outlineData.holes.Add(path);
                        goto result;
                    }

                outlineDataNext:
                    continue;
                }

                outlineDataList.Add(new OutlineData(path));

            result:
                continue;
            }

            CreateMeshData(outlineDataList, out var v, out var t);

            if (v.Count >= ushort.MaxValue)
            {
                Debug.LogErrorFormat($"Too many veretics! Sprite {sprite.name} has {v.Count} vertices.");
                vertices = sprite.vertices;
                triangles = sprite.triangles;
            }
            else
            {
                t.Reverse();
                vertices = v.ToArray();
                triangles = t.ToArray();
            }
        }

        private static Vector2[][] GenerateOutline(Sprite sprite, float detail, byte alphaTolerance, bool detectHoles)
        {
            detail = Mathf.Pow(detail, 2.5f);//TODO
            object[] parameters = new object[] { sprite, detail, alphaTolerance, detectHoles, null };
            _generateOutlineMethodInfo.Invoke(null, parameters);
            return (Vector2[][])parameters[4];
        }

        private static void CreateMeshData(List<OutlineData> outlineDataList, out List<Vector2> vertices, out List<ushort> triangles)
        {
            vertices = new List<Vector2>();
            triangles = new List<ushort>();

            foreach (OutlineData outlineData in outlineDataList)
            {
                outlineData.CreateMeshData(out var v, out var t, (ushort)vertices.Count);
                vertices.AddRange(v);
                triangles.AddRange(t);
            }
        }

        private static bool PolyContainsPoly(Vector2[] polyPoints, Vector2[] polyPoints2)
        {
            for (int i = 0; i < polyPoints2.Length; i++)
            {
                if (PolyContainsPoint(polyPoints, polyPoints2[i]) == false)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool PolyContainsPoint(Vector2[] polyPoints, Vector2 p)
        {
            int j = polyPoints.Length - 1;
            bool inside = false;
            
            for (int i = 0; i < polyPoints.Length; j = i++)
            {
                Vector2 pi = polyPoints[i];
                Vector2 pj = polyPoints[j];
                if (((pi.y <= p.y && p.y < pj.y) || (pj.y <= p.y && p.y < pi.y)) &&
                    (p.x < (pj.x - pi.x) * (p.y - pi.y) / (pj.y - pi.y) + pi.x))
                    inside = !inside;
            }

            return inside;
        }
    }
}
