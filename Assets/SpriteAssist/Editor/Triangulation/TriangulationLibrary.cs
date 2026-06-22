using UnityEngine;

namespace SpriteAssist
{
    public enum TriangulationLibrary
    {
        [InspectorName("LibTessDotNet")]
        LibTessDotNet = 0,
        [InspectorName("iShapeTriangulation")]
        IShape = 1,
    }
}
