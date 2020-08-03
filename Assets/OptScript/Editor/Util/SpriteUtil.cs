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

        public static void GetMeshData(Sprite sprite, SpriteConfigData data, out Vector2[] vertices, out ushort[] triangles, MeshRenderType meshRenderType)
        {
            if (data == null || !data.overriden)
            {
                vertices = sprite.vertices;
                triangles = sprite.triangles;
                return;
            }

            Vector2[][] paths = GenerateOutline(sprite, data, meshRenderType);
            CreateMeshData(paths, out vertices, out triangles);

            if (vertices.Length >= ushort.MaxValue)
            {
                Debug.LogErrorFormat($"Too many veretics! Sprite {sprite.name} has {vertices.Length} vertices.");
                vertices = sprite.vertices;
                triangles = sprite.triangles;
            }
        }

        private static Vector2[][] GenerateOutline(Sprite sprite, SpriteConfigData data, MeshRenderType meshRenderType)
        {
            switch (meshRenderType)
            {
                case MeshRenderType.Transparent:
                    return GenerateTransparentOutline(sprite, data.detail, data.alphaTolerance, data.detectHoles);
                    
                case MeshRenderType.Opaque:
                    return GenerateOpaqueOutline(sprite, data.opaqueAlphaTolerance, data.vertexMergeDistance);
                    
                case MeshRenderType.SeparatedTransparent:
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

                    return GenerateTransparentOutline(newSprite, data.detail, data.alphaTolerance, data.detectHoles);
            }

            return null;
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

        private static void CreateMeshData(Vector2[][] paths, out Vector2[] vertices, out ushort[] triangles)
        {
            Tess tess = new Tess();

            foreach (Vector2[] path in paths)
            {
                ContourVertex[] contour = new ContourVertex[path.Length];

                for (var i = 0; i < contour.Length; i++)
                {
                    contour[i].Position = new Vec3(path[i].x, path[i].y, 0);
                }

                tess.AddContour(contour, ContourOrientation.CounterClockwise);
            }

            tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);

            vertices = tess.Vertices.Select(v => new Vector2(v.Position.X, v.Position.Y)).ToArray();
            triangles = tess.Elements.Select(e => (ushort)e).ToArray();
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

        public static Vector2[] GetVerticeBasedUV(Sprite sprite, Vector2[] vertices)
        {
            var uv = new Vector2[vertices.Length];

            for (var i = 0; i < uv.Length; i++)
            {
                uv[i] = vertices[i] * sprite.pixelsPerUnit + sprite.pivot;
                uv[i].x /= sprite.texture.width;
                uv[i].y /= sprite.texture.height;
            }

            return uv;
        }

        public static void UpdateMesh(Sprite sprite, SpriteConfigData data, ref Mesh mesh, MeshRenderType meshRenderType)
        {
            GetMeshData(sprite, data, out var v, out var t, meshRenderType);
            Vector2[] uv = GetVerticeBasedUV(sprite, v);

            mesh.Clear();
            mesh.SetVertices(Array.ConvertAll(v, i => (Vector3)i).ToList());
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(Array.ConvertAll(t, i => (int)i), 0);
        }
    }
}
