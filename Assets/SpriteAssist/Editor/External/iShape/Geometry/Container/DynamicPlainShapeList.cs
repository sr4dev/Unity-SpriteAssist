using iShape.Collections;
using Unity.Collections;

namespace iShape.Geometry.Container {

    public struct DynamicPlainShapeList {
        
        public DynamicArray<IntVector> points;
        public DynamicArray<PathLayout> layouts;
        public DynamicArray<Segment> segments;
 
        public DynamicPlainShapeList(Allocator allocator) {
            this.points = new DynamicArray<IntVector>(0, allocator);
            this.layouts = new DynamicArray<PathLayout>(0, allocator);
            this.segments = new DynamicArray<Segment>(0, allocator);
        }

        public DynamicPlainShapeList(int minimumPointsCapacity, int minimumLayoutsCapacity, int minimumSegmentsCapacity, Allocator allocator) {
            this.points = new DynamicArray<IntVector>(minimumPointsCapacity, allocator);
            this.layouts = new DynamicArray<PathLayout>(minimumLayoutsCapacity, allocator);
            this.segments = new DynamicArray<Segment>(minimumSegmentsCapacity, allocator);            
        }

        public DynamicPlainShapeList(PlainShape shape, Allocator allocator) {
            this.points = new DynamicArray<IntVector>(shape.points, allocator);
            this.layouts = new DynamicArray<PathLayout>(shape.layouts, allocator);
            this.segments = new DynamicArray<Segment>(1, allocator);
            this.segments.Add(new Segment(0, shape.layouts.Length));
        }
        public void RemoveAll() {
            this.segments.RemoveAll();
            this.points.RemoveAll();
            this.layouts.RemoveAll();
        }
        
        public void Add(PlainShapeList list) {
            // TODO optimise
            int n = list.Count;
            for (int i = 0; i < n; ++i) {
                var shape = list.Get(i, Allocator.Temp);
                this.Add(shape);
                shape.Dispose();
            }
        }
        
        public void Add(PlainShape shape) {
            this.segments.Add(new Segment(this.layouts.Count, shape.layouts.Length));
            this.points.Add(shape.points);
            this.layouts.Add(shape.layouts);
        }
        
        public void Add(DynamicPlainShape shape) {
            this.segments.Add(new Segment(this.layouts.Count, shape.layouts.Count));
            this.points.Add(shape.points);
            this.layouts.Add(shape.layouts);
        }
        
        public void Add(NativeSlice<IntVector> path) {
            this.segments.Add(new Segment(this.layouts.Count, 1));
            this.points.Add(path);
            this.layouts.Add(new PathLayout(0, path.Length, true));
        }

        public PlainShape Get(int index, Allocator allocator) {
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

            int pointBegin = offset;
            var lastLayout = shapeLayouts[shapeLayouts.Length - 1];
            int pointLength = lastLayout.begin + lastLayout.length;

            var shapePoints = new NativeArray<IntVector>(pointLength, allocator);
            shapePoints.Slice(0, pointLength).CopyFrom(this.points.Slice(pointBegin, pointLength));
            
            return new PlainShape(shapePoints, shapeLayouts);
        }

        public void Dispose() {
            this.points.Dispose();
            this.layouts.Dispose();
            this.segments.Dispose();
        }

        public PlainShapeList Convert() {
            return new PlainShapeList(this.points.Convert(), this.layouts.Convert(), this.segments.Convert());
        }
    }

}