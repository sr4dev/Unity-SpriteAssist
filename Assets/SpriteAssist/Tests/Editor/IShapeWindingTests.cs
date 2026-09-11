using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace SpriteAssist.Tests
{
    public class IShapeWindingTests
    {
        private static readonly Vector2[] Hull =
        {
            new Vector2(0, 0), new Vector2(2, 0), new Vector2(2, 2), new Vector2(0, 2)
        };

        [TestCase(false, 3f)]
        [TestCase(true, 4f)]
        public void NestedAdditiveContour_RespectsWinding(bool nonZero, float expectedArea)
        {
            var inside = new[] { new Vector2(.5f, .5f), new Vector2(1.5f, .5f), new Vector2(1.5f, 1.5f), new Vector2(.5f, 1.5f) };
            Assert.That(Triangulate(new[] { Hull, inside }, nonZero), Is.EqualTo(expectedArea).Within(.002f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TouchingHole_IsPreserved(bool nonZero)
        {
            var touchingHole = new[] { new Vector2(0, 1), new Vector2(.5f, 1.5f), new Vector2(.5f, .5f) };
            Assert.That(Triangulate(new[] { Hull, touchingHole }, nonZero), Is.EqualTo(3.75f).Within(.002f));
        }

        [TestCase(false, 3f)]
        [TestCase(true, 3.5f)]
        public void OverlappingContours_RespectWinding(bool nonZero, float expectedArea)
        {
            var left = new[] { new Vector2(0, 0), new Vector2(2, 0), new Vector2(2, 1), new Vector2(0, 1) };
            var right = new[] { new Vector2(1, .5f), new Vector2(3, .5f), new Vector2(3, 1.5f), new Vector2(1, 1.5f) };
            Assert.That(Triangulate(new[] { left, right }, nonZero), Is.EqualTo(expectedArea).Within(.002f));
        }

        private static float Triangulate(Vector2[][] paths, bool nonZero)
        {
            var original = new Vector2[paths.Length][];
            for (int i = 0; i < paths.Length; i++) original[i] = (Vector2[])paths[i].Clone();
            // fallback でテストが通らないよう、iShape 実装を直接呼ぶ。
            Type type = typeof(TriangulationUtil).Assembly.GetType("SpriteAssist.TriangulatorIShape", true);
            object triangulator = Activator.CreateInstance(type, true);
            object[] args = { new SpriteConfigData { edgeSmoothing = 1, useNonZero = nonZero }, paths, null, null };
            bool success = (bool)type.GetMethod("TryTriangulate", BindingFlags.Public | BindingFlags.Instance).Invoke(triangulator, args);
            Assert.That(success, Is.True, "iShape itself must succeed");
            for (int i = 0; i < paths.Length; i++) Assert.That(paths[i], Is.EqualTo(original[i]), "Input outline must remain unchanged");
            var vertices = (Vector2[])args[2];
            var triangles = (ushort[])args[3];
            float area = 0;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector2 a = vertices[triangles[i]], b = vertices[triangles[i + 1]], c = vertices[triangles[i + 2]];
                area += Mathf.Abs((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) / 2;
            }
            return area;
        }
    }
}
