using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ClipperLib;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SpriteAssist.Tests
{
    public class ProvidedOutlineTests
    {
        private static readonly Vector2[] Hull =
        {
            new Vector2(0, 0), new Vector2(2, 0), new Vector2(2, 2), new Vector2(0, 2)
        };

        [TestCase(false)]
        [TestCase(true)]
        public void TouchingHole_KeepsBoundaryWithoutInset(bool nonZero)
        {
            Check(new[] { Hull, new[] { new Vector2(0, 1), new Vector2(.5f, 1.5f), new Vector2(.5f, .5f) } }, nonZero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void IntersectionsAndRepeatedVertices_KeepFilledRegion(bool nonZero)
        {
            Check(new[] { new[] { new Vector2(0, 0), new Vector2(2, 2), new Vector2(0, 2), new Vector2(2, 0) } }, nonZero);
            Check(new[] { new[] { new Vector2(0, 0), new Vector2(2, 0), new Vector2(2, 2), new Vector2(0, 2), new Vector2(0, 0), new Vector2(-1, 0), new Vector2(-1, -1), new Vector2(0, -1) } }, nonZero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NestedAndOverlappingContours_KeepConfiguredFill(bool nonZero)
        {
            var inside = new[] { new Vector2(.5f, .5f), new Vector2(1.5f, .5f), new Vector2(1.5f, 1.5f), new Vector2(.5f, 1.5f) };
            var island = new[] { new Vector2(.75f, .75f), new Vector2(1.25f, .75f), new Vector2(1.25f, 1.25f), new Vector2(.75f, 1.25f) };
            Check(new[] { Hull, inside }, nonZero);
            Check(new[] { Hull, inside.Reverse().ToArray(), island }, nonZero);
            Check(new[] { Hull, Hull.Select(p => p + new Vector2(1, 1)).ToArray() }, nonZero);
        }

        [Test]
        public void ThinFeatures_AreIndependentOfSmoothing()
        {
            var paths = new[] { new[] { new Vector2(0, 0), new Vector2(1, .0002f), new Vector2(2, 0), new Vector2(2, 2), new Vector2(1.0002f, 2), new Vector2(1, 1), new Vector2(1, 2), new Vector2(0, 2) } };
            Check(paths, false);
        }

        [Test]
        public void MoreThan1000Points_DoesNotSimplifyShortEdges()
        {
            var contour = Enumerable.Range(0, 1200).Select(i =>
            {
                float angle = i * Mathf.PI * 2 / 1200;
                float radius = i % 2 == 0 ? 1 : .995f;
                return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }).ToArray();
            Check(new[] { contour }, false);
        }

        [Test]
        public void EmptyPointAndLineContours_DoNotChangeFilledRegion()
        {
            Check(new[] { Hull, new[] { Vector2.zero, Vector2.zero, Vector2.zero }, new[] { Vector2.zero, Vector2.one, Vector2.one, Vector2.zero } }, false);
        }

        [Test]
        public void GridCollapse_FailsInsteadOfFallingBack()
        {
            var previous = SpriteAssistSettings.instance.defaultTriangulationLibrary;
            try
            {
                SpriteAssistSettings.instance.defaultTriangulationLibrary = TriangulationLibrary.IShape;
                var paths = new[] { Hull, new[] { new Vector2(3, 0), new Vector2(3.000001f, 0), new Vector2(3, .000001f) } };
                Assert.Throws<ArgumentException>(() => TriangulationUtil.TriangulateProvidedOutline(new SpriteConfigData(), paths, out _, out _));
            }
            finally { SpriteAssistSettings.instance.defaultTriangulationLibrary = previous; }
        }

        [TestCase(SpriteConfigData.Mode.UnityDefaultForTransparent)]
        [TestCase(SpriteConfigData.Mode.UnityDefaultForOpaque)]
        public void SavedOutlineImport_PreservesGeometryAndPersistentMesh(SpriteConfigData.Mode mode)
        {
            const string path = "Assets/SpriteAssistProvidedOutlineTemp.png";
            if (System.IO.File.Exists(path) || System.IO.File.Exists(path + ".meta"))
                throw new InvalidOperationException("Test path already exists: " + path);
            var outline = new[] { new Vector2(-8, -8), new Vector2(0, -7.9f), new Vector2(8, -8), new Vector2(8, 8), new Vector2(-8, 8) };
            try
            {
                Assert.That(AssetDatabase.CopyAsset("Assets/Example/Sprite/rebox-green-tri.png", path), Is.True);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                foreach (var key in importer.GetExternalObjectMap().Keys.ToArray()) importer.RemoveRemap(key);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = 0;
                settings.spritePivot = new Vector2(.5f, .5f);
                settings.spritePixelsPerUnit = 100;
                importer.SetTextureSettings(settings);
                var serialized = new SerializedObject(importer);
                var paths = serialized.FindProperty("m_SpriteSheet.m_Outline");
                paths.arraySize = 1;
                var contour = paths.GetArrayElementAtIndex(0);
                contour.arraySize = outline.Length;
                for (int i = 0; i < outline.Length; i++) contour.GetArrayElementAtIndex(i).vector2Value = outline[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Vector3[] firstVertices = null;
                int[] firstTriangles = null;
                Vector2[] firstUv = null;
                string firstIdentity = null;
                foreach (float smoothing in new[] { 0f, 1f })
                {
                    importer.userData = JsonUtility.ToJson(new SpriteConfigData { mode = mode, edgeSmoothing = smoothing });
                    importer.SaveAndReimport();
                    Assert.That(SpriteMeshAssets.TryGetMeshes(path, out var mesh, out _), Is.True);
                    Assert.That(EditorUtility.IsPersistent(mesh), Is.True);
                    Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string guid, out long fileId), Is.True);
                    AssertRegion(new[] { outline.Select(p => p / 100).ToArray() }, false,
                        mesh.vertices.Select(p => (Vector2)p).ToArray(), mesh.triangles.Select(i => (ushort)i).ToArray());
                    if (firstIdentity != null)
                    {
                        Assert.That(guid + ":" + fileId, Is.EqualTo(firstIdentity));
                        Assert.That(mesh.vertices, Is.EqualTo(firstVertices));
                        Assert.That(mesh.triangles, Is.EqualTo(firstTriangles));
                        Assert.That(mesh.uv, Is.EqualTo(firstUv));
                    }
                    firstIdentity = guid + ":" + fileId;
                    firstVertices = mesh.vertices;
                    firstTriangles = mesh.triangles;
                    firstUv = mesh.uv;
                    importer = (TextureImporter)AssetImporter.GetAtPath(path);
                }
            }
            finally { AssetDatabase.DeleteAsset(path); }
        }

        private static void Check(Vector2[][] paths, bool nonZero)
        {
            var input = paths.Select(p => (Vector2[])p.Clone()).ToArray();
            // iShape 自身を呼び、fallback による見かけの成功を防ぐ。
            var type = typeof(TriangulationUtil).Assembly.GetType("SpriteAssist.TriangulatorIShape", true);
            var method = type.GetMethod("TryTriangulateProvided", BindingFlags.Public | BindingFlags.Instance);
            var instance = Activator.CreateInstance(type, true);
            Vector2[] firstVertices = null;
            ushort[] firstTriangles = null;
            foreach (float smoothing in new[] { 0f, 1f })
            {
                object[] args = { new SpriteConfigData { edgeSmoothing = smoothing, useNonZero = nonZero }, paths, null, null };
                Assert.That((bool)method.Invoke(instance, args), Is.True);
                var vertices = (Vector2[])args[2];
                var triangles = (ushort[])args[3];
                AssertRegion(paths, nonZero, vertices, triangles);
                if (firstVertices != null)
                {
                    Assert.That(vertices, Is.EqualTo(firstVertices));
                    Assert.That(triangles, Is.EqualTo(firstTriangles));
                }
                firstVertices = vertices;
                firstTriangles = triangles;
            }
            for (int i = 0; i < paths.Length; i++) Assert.That(paths[i], Is.EqualTo(input[i]));
        }

        private static void AssertRegion(Vector2[][] paths, bool nonZero, Vector2[] vertices, ushort[] triangles)
        {
            var input = paths.Select(p => p.Select(ToInt).ToList()).ToList();
            var expected = Union(input, nonZero ? PolyFillType.pftNonZero : PolyFillType.pftEvenOdd);
            var faces = new List<List<IntPoint>>();
            double area = 0;
            int winding = 0;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var face = new List<IntPoint> { ToInt(vertices[triangles[i]]), ToInt(vertices[triangles[i + 1]]), ToInt(vertices[triangles[i + 2]]) };
                double signedArea = Clipper.Area(face);
                if (signedArea == 0) continue;
                if (winding != 0) Assert.That(Math.Sign(signedArea), Is.EqualTo(winding), "Triangle winding changed");
                winding = Math.Sign(signedArea);
                area += Math.Abs(signedArea);
                faces.Add(face);
            }
            var actual = Union(faces, PolyFillType.pftNonZero);
            var clipper = new Clipper();
            clipper.AddPaths(expected, PolyType.ptSubject, true);
            clipper.AddPaths(actual, PolyType.ptClip, true);
            var difference = new List<List<IntPoint>>();
            Assert.That(clipper.Execute(ClipType.ctXor, difference), Is.True);
            Assert.That(difference.Sum(p => Math.Abs(Clipper.Area(p))), Is.Zero, "Filled boundary changed on the integer grid");
            Assert.That(area, Is.EqualTo(Math.Abs(actual.Sum(Clipper.Area))), "Triangles overlap");
        }

        private static List<List<IntPoint>> Union(List<List<IntPoint>> paths, PolyFillType fill)
        {
            var clipper = new Clipper { StrictlySimple = true };
            clipper.AddPaths(paths, PolyType.ptSubject, true);
            var result = new List<List<IntPoint>>();
            Assert.That(clipper.Execute(ClipType.ctUnion, result, fill, fill), Is.True);
            return result;
        }

        private static IntPoint ToInt(Vector2 p) => new IntPoint((long)Mathf.Round(p.x * 10000), (long)Mathf.Round(p.y * 10000));
    }
}
