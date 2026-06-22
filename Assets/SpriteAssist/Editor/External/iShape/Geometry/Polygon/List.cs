using Unity.Collections;
using UnityEngine;

namespace iShape.Geometry.Polygon {

    public struct List {
        
        private NativeArray<Vector2> points;
        private NativeArray<Layout> layouts;
        
        public int Count => layouts.Length;
        
        public List(NativeArray<Vector2> points, NativeArray<Layout> layouts) {
            this.points = points;
            this.layouts = layouts;
        }
        
        public List(Allocator allocator) {
            this.points = new NativeArray<Vector2>(0, allocator);
            this.layouts = new NativeArray<Layout>(0, allocator);
        }

        public Polygon Get(int index, Allocator allocator) {
            var layout = this.layouts[index];
            var array = new NativeArray<Vector2>(layout.length, allocator);
            array.Slice(0, layout.length).CopyFrom(this.points.Slice(layout.begin, layout.length));

            return new Polygon(array, layout.form);
        }
        
        public NativeSlice<Vector2> Get(int index) {
            var layout = this.layouts[index];
            return this.points.Slice(layout.begin, layout.length);
        }

        public void Dispose() {
            this.points.Dispose();
            this.layouts.Dispose();
        }
    }

}