using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

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

        public static void GetMeshData(Sprite sprite, SpriteConfigData data, out Vector2[] vertices, out ushort[] triangles, bool isOpaque, bool isComplex)
        {
            if (data == null || !data.overriden)
            {
                vertices = sprite.vertices;
                triangles = sprite.triangles;
                return;
            }

            
            List<OutlineData> outlineDataList = new List<OutlineData>();
            Vector2[][] paths;
            if (isOpaque == false)
            {
                if (isComplex)
                {
                    //test
                    var tPixels = sprite.texture.GetPixels32();
                    var transparent = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);

                    for (int i = 0; i < tPixels.Length; i++)
                    {
                        if (tPixels[i].a >= data.opaqueAlphaTolerance)
                        {
                            tPixels[i].a = 0;
                        }
                    }

                    transparent.SetPixels32(tPixels);
                    transparent.Apply();
                    var normalizedPivot = new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);
                    var newSprite = Sprite.Create(transparent, sprite.rect, normalizedPivot, sprite.pixelsPerUnit, 1, SpriteMeshType.Tight);

                    paths = GenerateTransparentOutline(newSprite, data.detail, data.alphaTolerance, data.detectHoles);
                } else
                {
                    paths  = GenerateTransparentOutline(sprite, data.detail, data.alphaTolerance, data.detectHoles);
                }
            }
            else
            {
                paths = GenerateOpaqueOutline(sprite, data.opaqueAlphaTolerance, data.vertexMergeDistance);
            }
            
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

        private static Vector2[][] GenerateTransparentOutline(Sprite sprite, float detail, byte alphaTolerance, bool detectHoles)
        {
            detail = Mathf.Pow(detail, 2.5f);//TODO
            object[] parameters = new object[] { sprite, detail, alphaTolerance, detectHoles, null };
            _generateOutlineMethodInfo.Invoke(null, parameters);
            return (Vector2[][])parameters[4];
        }

        private static Vector2[][] GenerateOpaqueOutline(Sprite sprite, byte alphaTolerance, int mergeDistance)
        {
            MC_SimpleSurfaceEdge mcs = new MC_SimpleSurfaceEdge(sprite.texture.GetPixels(), sprite.texture.width, sprite.texture.height, (float)alphaTolerance / 255);
            mcs.MergeClosePoints(mergeDistance);

            List<MC_EdgeLoop> edges = mcs.edgeLoops;
            var newPaths = new Vector2[edges.Count][];
            var scale = 1 / sprite.pixelsPerUnit;
            var offsetX = -sprite.pivot.x;
            var offsetY = -sprite.pivot.y;
            for (int i = 0; i < edges.Count; i++)
            {
                newPaths[i] = edges[i].GetVertexList(scale, offsetX, offsetY);
            }

            return newPaths;
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

        public static void SeparateTextureForDebug(Sprite sprite, byte alphaTolerance)
        {
            var tPixels = sprite.texture.GetPixels32();
            var oPixels = tPixels.ToArray();
            var width = (int)sprite.rect.width;
            var height = (int)sprite.rect.height;
            var transparent = new Texture2D(width, height);
            var opaque = new Texture2D(width, height);

            for (int i = 0; i < tPixels.Length; i++)
            {
                if (tPixels[i].a >= alphaTolerance)
                {
                    tPixels[i].a = 0;
                }
                else
                {
                    oPixels[i].a = 0;
                }
            }

            ReduceNoise(ref tPixels, width, height);
            ReduceNoise(ref tPixels, width, height);
            ReduceNoise(ref tPixels, width, height);


            transparent.SetPixels32(tPixels);
            transparent.Apply();

            opaque.SetPixels32(oPixels);
            opaque.Apply();

            //var tSprite = Sprite.Create(transparent, sprite.rect, sprite.pivot);
            //var oSprite = Sprite.Create(opaque, sprite.rect, sprite.pivot);

            //AssetDatabase.CreateAsset(transparent, "Assets/tSprite.asset");
            //AssetDatabase.CreateAsset(opaque, "Assets/oSprite.asset");

            Test(transparent, sprite.name + "_trans");
            Test(opaque, sprite.name + "_opaque");

            AssetDatabase.Refresh();
        }

        public static void Test(Texture2D texture, string fileName)
        {
            //then Save To Disk as PNG
            byte[] bytes = texture.EncodeToPNG();
            var dirPath = Application.dataPath + "/";
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            File.WriteAllBytes(dirPath + fileName + ".png", bytes);
        }

        private static void ReduceNoise(ref Color32[] tPixels, int height, int width)
        {
            int c = 0;
            for (int i = 0; i < tPixels.Length; i++)
            {
                var x = i / width;
                var y = i % width;

                {
                    if (tPixels[i].a > 0)
                    {
                        var up = GetCol(tPixels, (x - 1) * width + y);
                        var down = GetCol(tPixels, (x + 1) * width + y);
                        var left = GetCol(tPixels, x * width + y - 1);
                        var right = GetCol(tPixels, x * width + y + 1);

                        if (up.a == 0 && down.a == 0 && left.a == 0 && right.a == 0)
                        {
                            tPixels[i].a = 0;
                            c++;
                        }
                    }
                }
            }

            Debug.Log(width * height + " == " + tPixels.Length + ", " + c);
        }

        private static Color GetCol(Color32[] c, int i)
        {
            if (c.Length > i && i >= 0)
            {
                return c[i];
            }

            return new Color();
        }

        public static void GetUV(Sprite sprite, Vector2[] vertices, out Vector2[] uv)
        {
            uv = new Vector2[vertices.Length];

            for (var i = 0; i < uv.Length; i++)
            {
                uv[i] = vertices[i] * sprite.pixelsPerUnit + sprite.pivot;
                uv[i].x /= sprite.texture.width;
                uv[i].y /= sprite.texture.height;
            }
        }

        public static Mesh CreateMesh(Sprite sprite, SpriteConfigData data, string meshName)
        {
            Mesh mesh = new Mesh
            {
                name = meshName
            };
            SpriteUtil.UpdateMesh(sprite, data, ref mesh, false, false);
            return mesh;
        }

        public static void UpdateMesh(Sprite sprite, SpriteConfigData data, ref Mesh mesh, bool isOpaque, bool isComplex)
        {
            GetMeshData(sprite, data, out var v, out var t, isOpaque, isComplex);
            GetUV(sprite, v, out Vector2[] uv);

            mesh.Clear();
            mesh.SetVertices(Array.ConvertAll(v, i => (Vector3)i).ToList());
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(Array.ConvertAll(t, i => (int)i), 0);
        }
    }
}
