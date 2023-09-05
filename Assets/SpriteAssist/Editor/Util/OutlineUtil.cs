using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist
{
    public static class OutlineUtil
    {
        private static readonly MethodInfo _generateOutlineMethodInfo = typeof(UnityEditor.Sprites.SpriteUtility).GetMethod("GenerateOutlineFromSprite", BindingFlags.NonPublic | BindingFlags.Static);
        private const float FLOAT_TO_INT_SCALE = 1000f;
        private const float INT_TO_FLOAT_SCALE = 1 / FLOAT_TO_INT_SCALE;
        private const float EXTRUDE_SCALE = -500;

        public enum TranslateDirection
        {
            Left,
            Up,
            Down,
            Right
        }

        public static Vector2[][] GenerateOutline(Sprite sprite, SpriteConfigData data, MeshRenderType meshRenderType)
        {
            switch (meshRenderType)
            {
                case MeshRenderType.Transparent:
                    return GenerateTransparentOutline(sprite, data.transparentDetail, data.transparentAlphaTolerance, data.detectHoles);

                case MeshRenderType.Opaque:
                    return GenerateOpaqueOutline(sprite, data.opaqueDetail, data.opaqueAlphaTolerance, data.opaqueExtrude);

                case MeshRenderType.OpaqueWithoutExtrude:
                    return GenerateOpaqueOutline(sprite, data.opaqueDetail, data.opaqueAlphaTolerance, 0);

                case MeshRenderType.SeparatedTransparent:
                    return GenerateSeparatedTransparent(sprite, data);

                case MeshRenderType.OpaqueWithoutTightGrid:
                    return GenerateOpaqueWithoutTightGridOutline(sprite, data);

                case MeshRenderType.Grid:
                    return GenerateGridOutline(sprite, data.gridSize, data.gridTolerance, data.detectHoles);

                case MeshRenderType.TightGrid:
                    return GenerateTightGridOutline(sprite, data.gridSize, data.gridTolerance);
                    
                case MeshRenderType.Pixel:
                    return GeneratePixelEdgeOutline(sprite, data.gridSize, data.gridTolerance, false);

                default:
                    return Array.Empty<Vector2[]>();
            }
        }

        public static void Translate(TextureImporter textureImporter, TranslateDirection direction)
        {
            var serializedObject = new SerializedObject(textureImporter);
            var outlineSP = GetOutlineProperty(serializedObject, SpriteImportMode.Single, 0);
            var outlines = GetOutlines(outlineSP);

            if (outlines.Count == 0)
            {
                return;
            }

            var offset = new Vector2();

            switch (direction)
            {
                case TranslateDirection.Left:
                    offset.x--;
                    break;
                case TranslateDirection.Up:
                    offset.y++;
                    break;
                case TranslateDirection.Down:
                    offset.y--;
                    break;
                case TranslateDirection.Right:
                    offset.x++;
                    break;
            }
            
            foreach (var outline in outlines)
            {
                for (int i = 0; i < outline.Length; i++)
                {
                    outline[i] += offset;
                }
            }

            SetOutlines(outlineSP, outlines);
            serializedObject.ApplyModifiedProperties();

            var selection = Selection.activeObject;
            AssetDatabase.ImportAsset(textureImporter.assetPath, ImportAssetOptions.DontDownloadFromCacheServer);
            Selection.activeObject = selection;
        }

        public static void Resize(TextureImporter textureImporter, int originalWidth, int originalHeight, int newWidth, int newHeight, ResizeUtil.ResizeMethod resizeMethod)
        {
            var serializedObject = new SerializedObject(textureImporter);
            var outlineSP = GetOutlineProperty(serializedObject, SpriteImportMode.Single, 0);
            var outlines = GetOutlines(outlineSP);

            if (outlines.Count == 0)
            {
                return;
            }

            switch (resizeMethod)
            {
                case ResizeUtil.ResizeMethod.Scale:
                    var diffScale = new Vector2((float)newWidth / originalWidth, (float)newHeight / originalHeight);

                    foreach (var outline in outlines)
                    {
                        for (int i = 0; i < outline.Length; i++)
                        {
                            outline[i] *= diffScale;
                        }
                    }
                    break;

                case ResizeUtil.ResizeMethod.AddAlphaOrCropArea:
                    var diff = new Vector2(newWidth - originalWidth, newHeight - originalHeight) * 0.5f;

                    foreach (var outline in outlines)
                    {
                        for (int i = 0; i < outline.Length; i++)
                        {
                            outline[i] -= diff;
                        }
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(resizeMethod), resizeMethod, null);
            }

            SetOutlines(outlineSP, outlines);
            serializedObject.ApplyModifiedProperties();

            var selection = Selection.activeObject;
            AssetDatabase.ImportAsset(textureImporter.assetPath, ImportAssetOptions.DontDownloadFromCacheServer);
            Selection.activeObject = selection;
        }

        private static Vector2[][] GenerateGridOutline(Sprite sprite, int gridSize, float tolerance, bool dataDetectHoles)
        {
            Texture2D texture = sprite.texture;
            Vector2 offset = new Vector2(texture.width, texture.height) * -sprite.GetNormalizedPivot();
            float pixelsPerUnit = sprite.pixelsPerUnit;
            int unitCountX = Mathf.CeilToInt((float)texture.width / gridSize);
            int unitCountY = Mathf.CeilToInt((float)texture.height / gridSize);

            List<Vector2> gridOutline = new List<Vector2>(unitCountX * unitCountY * 6);
            
            for (int unitX = 0; unitX < unitCountX; unitX++)
            {
                for (int unitY = 0; unitY < unitCountY; unitY++)
                {
                    RectInt rect = new RectInt(unitX * gridSize, unitY * gridSize, gridSize, gridSize);
                    rect.width = Mathf.Min(rect.width, texture.width - rect.x);
                    rect.height = Mathf.Min(rect.height, texture.height - rect.y);

                    if (dataDetectHoles && !HasGrid(texture, rect, tolerance))
                    {
                        continue;
                    }
                    
                    gridOutline.Add((new Vector2(rect.xMin, rect.yMin) + offset) / pixelsPerUnit);
                    gridOutline.Add((new Vector2(rect.xMin, rect.yMax) + offset) / pixelsPerUnit);
                    gridOutline.Add((new Vector2(rect.xMax, rect.yMin) + offset) / pixelsPerUnit);
                    
                    gridOutline.Add((new Vector2(rect.xMax, rect.yMin) + offset) / pixelsPerUnit);
                    gridOutline.Add((new Vector2(rect.xMin, rect.yMax) + offset) / pixelsPerUnit);
                    gridOutline.Add((new Vector2(rect.xMax, rect.yMax) + offset) / pixelsPerUnit);
                }
            }

            return new[] {gridOutline.ToArray()};
        }

        private static Vector2[][] GenerateOpaqueWithoutTightGridOutline(Sprite sprite, SpriteConfigData data)
        {
            Vector2[][] opaquePaths = GenerateOpaqueOutline(sprite, data.opaqueDetail, data.opaqueAlphaTolerance, data.opaqueExtrude);
            Vector2[][] tightGridPaths = GeneratePixelEdgeOutline(sprite, data.gridSize, data.gridTolerance, true);
            List<List<IntPoint>> convertedOpaquePaths = ConvertToIntPointList(opaquePaths, FLOAT_TO_INT_SCALE);
            List<List<IntPoint>> convertedTightGridPaths = ConvertToIntPointList(tightGridPaths, FLOAT_TO_INT_SCALE);
            List<List<IntPoint>> intersectionPaths = new List<List<IntPoint>>();
            Clipper clipper = new Clipper();
            clipper.AddPaths(convertedOpaquePaths, PolyType.ptSubject, true);
            clipper.AddPaths(convertedTightGridPaths, PolyType.ptClip, true);
            clipper.Execute(ClipType.ctDifference, intersectionPaths, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);
            return ConvertToVector2Array(intersectionPaths, INT_TO_FLOAT_SCALE);
        }

        private static Vector2[][] GenerateTightGridOutline(Sprite sprite, int gridSize, float tolerance)
        {
            Texture2D texture = sprite.texture;
            Vector2 offset = new Vector2(texture.width, texture.height) * -sprite.GetNormalizedPivot();
            float pixelsPerUnit = sprite.pixelsPerUnit;
            int unitCountX = Mathf.CeilToInt((float)texture.width / gridSize);
            int unitCountY = Mathf.CeilToInt((float)texture.height / gridSize);

            List<Vector2> gridOutline = new List<Vector2>(unitCountX * unitCountY * 6);

            for (int unitX = 0; unitX < unitCountX; unitX++)
            {
                for (int unitY = 0; unitY < unitCountY; unitY++)
                {
                    RectInt rect = new RectInt(unitX * gridSize, unitY * gridSize, gridSize, gridSize);
                    rect.width = Mathf.Min(rect.width, texture.width - rect.x);
                    rect.height = Mathf.Min(rect.height, texture.height - rect.y);

                    if (!HasTightGrid(texture, rect, tolerance))
                    {
                        continue;
                    }

                    gridOutline.Add((new Vector2(rect.xMin, rect.yMin) + offset) / pixelsPerUnit);
                    gridOutline.Add((new Vector2(rect.xMin, rect.yMax) + offset) / pixelsPerUnit);
                    gridOutline.Add((new Vector2(rect.xMax, rect.yMin) + offset) / pixelsPerUnit);

                    gridOutline.Add((new Vector2(rect.xMax, rect.yMin) + offset) / pixelsPerUnit);
                    gridOutline.Add((new Vector2(rect.xMin, rect.yMax) + offset) / pixelsPerUnit);
                    gridOutline.Add((new Vector2(rect.xMax, rect.yMax) + offset) / pixelsPerUnit);
                }
            }

            return new[] { gridOutline.ToArray() };
        }

        private static Vector2[][] GeneratePixelEdgeOutline(Sprite sprite, int gridSize, float tolerance, bool isTightGrid)
        {
            Texture2D texture = sprite.texture;
            Vector2 offset = new Vector2(texture.width, texture.height) * -sprite.GetNormalizedPivot();
            float pixelsPerUnit = sprite.pixelsPerUnit;
            int unitCountX = Mathf.CeilToInt((float)texture.width / gridSize);
            int unitCountY = Mathf.CeilToInt((float)texture.height / gridSize);

            List<List<IntPoint>> pixelEdges = new List<List<IntPoint>>(unitCountX * unitCountY);
            Func<Texture2D, RectInt, float, bool> hasGrid;

            if (isTightGrid)
            {
                hasGrid = HasTightGrid;
            }
            else
            {
                hasGrid = HasGrid;
            }

            for (int unitX = 0; unitX < unitCountX; unitX++)
            {
                List<IntPoint> intPoints = null;
                RectInt lastRect = default;

                for (int unitY = 0; unitY < unitCountY; unitY++)
                {
                    RectInt rect = new RectInt(unitX * gridSize, unitY * gridSize, gridSize, gridSize);
                    rect.width = Mathf.Min(rect.width, texture.width - rect.x);//crop
                    rect.height = Mathf.Min(rect.height, texture.height - rect.y);//crop

                    if (hasGrid(texture, rect, tolerance))
                    {
                        if (intPoints == null)
                        {
                            //open
                            intPoints = new List<IntPoint>(4);
                            intPoints.Add(new IntPoint(rect.xMax, rect.yMin));
                            intPoints.Add(new IntPoint(rect.xMin, rect.yMin));
                            pixelEdges.Add(intPoints);
                        }

                        if (unitY == unitCountY - 1)
                        {
                            //force close
                            intPoints.Add(new IntPoint(rect.xMin, rect.yMax));
                            intPoints.Add(new IntPoint(rect.xMax, rect.yMax));
                            intPoints = null;
                        }
                    }
                    else
                    {
                        if (intPoints != null)
                        {
                            //close
                            intPoints.Add(new IntPoint(rect.xMin, lastRect.yMax));
                            intPoints.Add(new IntPoint(rect.xMax, lastRect.yMax));
                            intPoints = null;
                        }
                    }

                    lastRect = rect;
                }
            }

            List<List<IntPoint>> unionPixelEdges = new List<List<IntPoint>>(pixelEdges.Count);
            Clipper clipper = new Clipper();
            clipper.AddPaths(pixelEdges, PolyType.ptSubject, true);
            clipper.Execute(ClipType.ctUnion, unionPixelEdges, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);
            unionPixelEdges.TrimExcess();
            return ConvertToVector2Array(unionPixelEdges, 1 / pixelsPerUnit, offset);
        }

        private static bool HasGrid(Texture2D texture, RectInt rect, float tolerance)
        {
            int endX = rect.x + rect.width;
            int endY = rect.y + rect.height;
            for (int y = rect.y; y < endY; y++)
                for (int x = rect.x; x < endX; x++)
                    if (texture.GetPixel(x, y).a > tolerance)
                        return true;
            return false;
        }
        
        private static bool HasTightGrid(Texture2D texture, RectInt rect, float tolerance)
        {
            int endX = rect.x + rect.width;
            int endY = rect.y + rect.height;
            for (int y = rect.y; y < endY; y++)
            for (int x = rect.x; x < endX; x++)
                if (texture.GetPixel(x, y).a <= tolerance)
                    return false;

            return true;
        }

        private static Vector2[][] GenerateTransparentOutline(Sprite sprite, float detail, byte alphaTolerance, bool detectHoles)
        {
            float newDetail = Mathf.Pow(detail, 3);
            object[] parameters = new object[] { sprite, newDetail, alphaTolerance, detectHoles, null };
            _generateOutlineMethodInfo.Invoke(null, parameters);
            return (Vector2[][])parameters[4];
        }
        
        private static Vector2[][] GenerateOpaqueOutline(Sprite sprite, float detail, byte alphaTolerance, double extrude)
        {
            float newDetail = 0.5f + detail * 0.5f;
            Vector2[][] paths = GenerateTransparentOutline(sprite, newDetail, alphaTolerance, true);

            if (extrude <= 0)
            {
                return paths;
            }

            double newExtrude = extrude * EXTRUDE_SCALE;
            List<List<IntPoint>> intPointList = ConvertToIntPointList(paths, FLOAT_TO_INT_SCALE, detail);
            List<List<IntPoint>> offsetIntPointList = new List<List<IntPoint>>();
            ClipperOffset offset = new ClipperOffset();
            offset.AddPaths(intPointList, JoinType.jtMiter, EndType.etClosedPolygon);
            offset.Execute(ref offsetIntPointList, newExtrude);
            return ConvertToVector2Array(offsetIntPointList, INT_TO_FLOAT_SCALE);
        }

        private static Vector2[][] GenerateSeparatedTransparent(Sprite sprite, SpriteConfigData data)
        {
            Vector2[][] transparentPaths = GenerateTransparentOutline(sprite, data.transparentDetail, data.transparentAlphaTolerance, data.detectHoles);
            Vector2[][] opaquePaths = GenerateOpaqueOutline(sprite, data.opaqueDetail, data.opaqueAlphaTolerance, data.opaqueExtrude);
            List<List<IntPoint>> convertedTransparentPaths = ConvertToIntPointList(transparentPaths, FLOAT_TO_INT_SCALE);
            List<List<IntPoint>> convertedOpaquePaths = ConvertToIntPointList(opaquePaths, FLOAT_TO_INT_SCALE);
            List<List<IntPoint>> intersectionPaths = new List<List<IntPoint>>();
            Clipper clipper = new Clipper();
            clipper.AddPaths(convertedTransparentPaths, PolyType.ptSubject, true);
            clipper.AddPaths(convertedOpaquePaths, PolyType.ptClip, true);
            clipper.Execute(ClipType.ctDifference, intersectionPaths, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);
            return ConvertToVector2Array(intersectionPaths, INT_TO_FLOAT_SCALE);
        }

        private static List<List<IntPoint>> ConvertToIntPointList(Vector2[][] paths, float scale, float simplify)
        {
            float newSimplify = Mathf.Clamp01(1 - (simplify * 0.01f + 0.99f));
            List<List<IntPoint>> intPointPaths = new List<List<IntPoint>>(paths.Length);

            for (int i = 0; i < paths.Length; i++)
            {
                List<Vector2> simplifiedPath = new List<Vector2>(paths.Length);
                LineUtility.Simplify(paths[i].ToList(), newSimplify, simplifiedPath);
                List<IntPoint> intPointPath = new List<IntPoint>(simplifiedPath.Count);

                for (int j = 0; j < simplifiedPath.Count; j++)
                {
                    Vector2 point = simplifiedPath[j] * scale;
                    intPointPath.Add(new IntPoint(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y)));
                }

                intPointPaths.Add(intPointPath);
            }

            return intPointPaths;
        }

        private static List<List<IntPoint>> ConvertToIntPointList(Vector2[][] paths, float scale = 1, Vector2 offset = default)
        {
            List<List<IntPoint>> intPointPaths = new List<List<IntPoint>>(paths.Length);

            for (int i = 0; i < paths.Length; i++)
            {
                Vector2[] path = paths[i];
                List<IntPoint> intPointPath = new List<IntPoint>(path.Length);

                for (int j = 0; j < path.Length; j++)
                {
                    Vector2 point = (path[j] + offset) * scale;
                    intPointPath.Add(new IntPoint(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y)));
                }

                intPointPaths.Add(intPointPath);
            }

            return intPointPaths;
        }
        
        private static Vector2[][] ConvertToVector2Array(List<List<IntPoint>> intPointPaths, float scale = 1, Vector2 offset = default)
        {
            Vector2[][] outPaths = new Vector2[intPointPaths.Count][];

            for (int i = 0; i < intPointPaths.Count; i++)
            {
                List<IntPoint> intPointPath = intPointPaths[i];
                Vector2[] points = new Vector2[intPointPath.Count];

                for (int j = 0; j < intPointPath.Count; j++)
                {
                    IntPoint intPoint = intPointPath[j];
                    points[j] = (new Vector2(intPoint.X, intPoint.Y) + offset) * scale;
                }

                outPaths[i] = points;
            }

            return outPaths;
        }

        private static SerializedProperty GetOutlineProperty(SerializedObject importer, SpriteImportMode mode, int index)
        {
            return mode == SpriteImportMode.Multiple ? importer.FindProperty("m_SpriteSheet.m_Sprites").GetArrayElementAtIndex(index).FindPropertyRelative("m_Outline") : importer.FindProperty("m_SpriteSheet.m_Outline");
        }

        public static bool HasOutline(TextureImporter textureImporter)
        {
            var serializedObject = new SerializedObject(textureImporter);
            var outlineSP = GetOutlineProperty(serializedObject, SpriteImportMode.Single, 0);
            var outlines = GetOutlines(outlineSP);
            return outlines.Count != 0;
        }

        private static List<Vector2[]> GetOutlines(SerializedProperty outlineSP)
        {
            var outline = new List<Vector2[]>();
            if (outlineSP.arraySize > 0)
            {
                var outlinePathSP = outlineSP.GetArrayElementAtIndex(0);
                for (int j = 0; j < outlineSP.arraySize; ++j, outlinePathSP.Next(false))
                {
                    var o = new Vector2[outlinePathSP.arraySize];
                    if (o.Length > 0)
                    {
                        var psp = outlinePathSP.GetArrayElementAtIndex(0);
                        for (int k = 0; k < outlinePathSP.arraySize; ++k, psp.Next(false))
                        {
                            o[k] = psp.vector2Value;
                        }
                    }

                    outline.Add(o);
                }
            }

            return outline;
        }

        private static void SetOutlines(SerializedProperty outlineSP, List<Vector2[]> outline)
        {
            outlineSP.arraySize = outline.Count;
            if (outline.Count > 0)
            {
                var outlinePathSP = outlineSP.GetArrayElementAtIndex(0);
                for (int j = 0; j < outline.Count; ++j, outlinePathSP.Next(false))
                {
                    var o = outline[j];
                    outlinePathSP.arraySize = o.Length;
                    if (o.Length > 0)
                    {
                        var psp = outlinePathSP.GetArrayElementAtIndex(0);
                        for (int k = 0; k < o.Length; ++k, psp.Next(false))
                        {
                            psp.vector2Value = o[k];
                        }
                    }
                }
            }
        }

    }
}
