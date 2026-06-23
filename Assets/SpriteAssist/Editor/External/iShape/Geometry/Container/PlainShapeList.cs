using Unity.Collections;

namespace iShape.Geometry.Container {

    public struct PlainShapeList {

        public NativeArray<IntVector> points;
        public NativeArray<PathLayout> layouts;
        public NativeArray<Segment> segments;
        
        public int Count => segments.Length;
        
        public PlainShapeList(Allocator allocator) {
            this.points = new NativeArray<IntVector>(0, allocator);
            this.layouts = new NativeArray<PathLayout>(0, allocator);
            this.segments = new NativeArray<Segment>(0, allocator);
        }

        public PlainShapeList(NativeArray<IntVector> points, NativeArray<PathLayout> layouts, NativeArray<Segment> segments) {
            this.points = points;
            this.layouts = layouts;
            this.segments = segments;
        }
        
        public PlainShapeList(PlainShape plainShape, Allocator allocator) {
            this.points = new NativeArray<IntVector>(plainShape.points, allocator);
            this.layouts = new NativeArray<PathLayout>(plainShape.layouts, allocator);
            this.segments = new NativeArray<Segment>(1, allocator);
            this.segments[0] = new Segment(0, plainShape.layouts.Length);
        }
        

        public PlainShapeList(NativeArray<IntVector> points, bool isClockWise, Allocator allocator) {
            this.points = new NativeArray<IntVector>(points, allocator);
            this.layouts = new NativeArray<PathLayout>(1, allocator); 
            this.layouts[0] = new PathLayout(0, points.Length, isClockWise);
            this.segments = new NativeArray<Segment>(1, allocator);
            this.segments[0] = new Segment(0, 1);
        }
        
        public PlainShapeList(DynamicPlainShape plainShape, Allocator allocator) {
            this.segments = new NativeArray<Segment>(1, allocator) {[0] = new Segment(0, 1)};
            this.points = plainShape.points.ToArray(allocator);
            this.layouts = plainShape.layouts.ToArray(allocator);
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

            // shapeLayouts[0].begin === 0
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
    }

}