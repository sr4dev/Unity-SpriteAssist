using Unity.Collections;
using UnityEngine;

namespace iShape.Geometry.Container {

    public struct PathList {
        
        public NativeArray<Vector2> points;
        public NativeArray<PathLayout> layouts;
        public int Count => layouts.Length;

        public PathList(NativeArray<Vector2> points, NativeArray<PathLayout> layouts) {
            this.points = points;
            this.layouts = layouts;
        }
        
        public PathList(Allocator allocator) {
            this.points = new NativeArray<Vector2>(0, allocator);
            this.layouts = new NativeArray<PathLayout>(0, allocator);
        }

        public PathList(NativeArray<Vector2> points, bool isClockWise, Allocator allocator) {
            this.points = new NativeArray<Vector2>(points.Length, allocator);
            this.points.CopyFrom(points);
            this.layouts = new NativeArray<PathLayout>(1, allocator);
            this.layouts[0] = new PathLayout(0, points.Length, isClockWise);
        }

        public NativeArray<Vector2> Get(int index, Allocator allocator) {
            var layout = this.layouts[index];
            var array = new NativeArray<Vector2>(layout.length, allocator);
            array.Slice(0, layout.length).CopyFrom(this.points.Slice(layout.begin, layout.length));
            return array;
        }
        
        public NativeSlice<Vector2> Get(int index) {
            var layout = this.layouts[index];
            return this.points.Slice(layout.begin, layout.length);
        }

        public void Dispose() {
            this.points.Dispose();
            this.layouts.Dispose();
        }
        
        public Vector2[][] ToPaths() {
            int n = this.layouts.Length;
            var paths = new Vector2[n][];
            for (int i = 0; i < n; ++i) {
                var layout = this.layouts[i];
                var slice = points.Slice(layout.begin, layout.length);
                paths[i] = slice.ToArray();
            }
            return paths;
        }
    }

}