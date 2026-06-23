using iShape.Collections;
using Unity.Collections;
using UnityEngine;

namespace iShape.Geometry.Polygon {

    public struct DynamicList {
        
        private DynamicArray<Vector2> points;
        private DynamicArray<Layout> layouts;
        
        public DynamicList(int pointsCapacity, int layoutsCapacity, Allocator allocator) {
            this.points = new DynamicArray<Vector2>(pointsCapacity, allocator);
            this.layouts = new DynamicArray<Layout>(layoutsCapacity, allocator);
        }
        
        public void Add(Polygon polygon) {
            var layout = new Layout(points.Count, polygon.points.Length, polygon.form);
            this.points.Add(polygon.points);
            this.layouts.Add(layout);
        }

        public List Convert() {
            return new List(this.points.Convert(), this.layouts.Convert());
        }
        
        public void Dispose() {
            this.points.Dispose();
            this.layouts.Dispose();
        }
    }

}