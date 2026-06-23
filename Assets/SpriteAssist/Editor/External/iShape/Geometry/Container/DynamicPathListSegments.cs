using iShape.Collections;
using Unity.Collections;
using UnityEngine;

namespace iShape.Geometry.Container {

    public struct DynamicPathListSegments {
          
        public DynamicArray<Vector2> points;
        public DynamicArray<PathLayout> layouts;
        public DynamicArray<Segment> segments;
 
        public DynamicPathListSegments(Allocator allocator) {
            this.points = new DynamicArray<Vector2>(0, allocator);
            this.layouts = new DynamicArray<PathLayout>(0, allocator);
            this.segments = new DynamicArray<Segment>(0, allocator);
        }

        public DynamicPathListSegments(int minimumPointsCapacity, int minimumLayoutsCapacity, int minimumSegmentsCapacity, Allocator allocator) {
            this.points = new DynamicArray<Vector2>(minimumPointsCapacity, allocator);
            this.layouts = new DynamicArray<PathLayout>(minimumLayoutsCapacity, allocator);
            this.segments = new DynamicArray<Segment>(minimumSegmentsCapacity, allocator);            
        }
        
        public DynamicPathListSegments(PathList pathList, Allocator allocator) {
            this.points = new DynamicArray<Vector2>(pathList.points, allocator);
            this.layouts = new DynamicArray<PathLayout>(pathList.layouts, allocator);
            this.segments = new DynamicArray<Segment>(1, allocator);
            this.segments.Add(new Segment(0, pathList.layouts.Length));
        }
        
        public void Add(PathList pathList) {
            this.segments.Add(new Segment(this.layouts.Count, pathList.layouts.Length));
            this.points.Add(pathList.points);
            this.layouts.Add(pathList.layouts);
        }
        
        public void RemoveAll() {
            this.segments.RemoveAll();
            this.points.RemoveAll();
            this.layouts.RemoveAll();
        }

        public PathList Get(int index, Allocator allocator) {
            var segment = this.segments[index];
            var shapeLayouts = new NativeArray<PathLayout>(segment.length, allocator);
            shapeLayouts.Slice(0, segment.length).CopyFrom(this.layouts.Slice(segment.begin, segment.length));

            int offset = 0;
            if (index > 0) {
                for(int i = 0; i < index; ++i) {
                    var s = this.segments[i];
                    var l = this.layouts[s.end];
                    offset += l.begin + l.length;
                }
            }

            // shapeLayouts[0].begin === 0
            int pointBegin = offset;
            var lastLayout = shapeLayouts[shapeLayouts.Length - 1];
            int pointLength = lastLayout.begin + lastLayout.length;

            var pathList = new NativeArray<Vector2>(pointLength, allocator);
            pathList.Slice(0, pointLength).CopyFrom(this.points.Slice(pointBegin, pointLength));
            
            return new PathList(pathList, shapeLayouts);
        }

        public void Dispose() {
            this.points.Dispose();
            this.layouts.Dispose();
            this.segments.Dispose();
        }
    }

}