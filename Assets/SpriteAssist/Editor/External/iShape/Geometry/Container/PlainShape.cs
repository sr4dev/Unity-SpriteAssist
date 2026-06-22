using Unity.Collections;

namespace iShape.Geometry.Container {

    public struct PlainShape {

        public NativeArray<IntVector> points;
        public NativeArray<PathLayout> layouts;
        public int Count => this.layouts.Length;

        public long RootArea => Area(this.Get(0));

        public PlainShape(NativeArray<IntVector> points, NativeArray<PathLayout> layouts) {
            this.points = points;
            this.layouts = layouts;
        }
        
        public PlainShape(IntVector[] points, PathLayout[] layouts, Allocator allocator) {
            this.points = new NativeArray<IntVector>(points, allocator);
            this.layouts = new NativeArray<PathLayout>(layouts, allocator);
        }
        
        public PlainShape(Allocator allocator) {
            this.points = new NativeArray<IntVector>(0, allocator);
            this.layouts = new NativeArray<PathLayout>(0, allocator);
        }
        
        public PlainShape(PlainShape plainShape, Allocator allocator) {
            this.points = new NativeArray<IntVector>(plainShape.points, allocator);
            this.layouts = new NativeArray<PathLayout>(plainShape.layouts, allocator);
        }

        public PlainShape(NativeArray<IntVector> points, bool isClockWise, Allocator allocator) {
            this.points = new NativeArray<IntVector>(points.Length, allocator);
            this.points.CopyFrom(points);
            this.layouts = new NativeArray<PathLayout>(1, allocator);
            this.layouts[0] = new PathLayout(0, points.Length, isClockWise);
        }

        public PlainShape(IntShape iShape, Allocator allocator) {
            var count = iShape.hull.Length;

            for(int j = 0; j < iShape.holes.Length; ++j) {
                count += iShape.holes[j].Length;
            }

            this.points = new NativeArray<IntVector>(count, allocator);
            this.layouts = new NativeArray<PathLayout>(iShape.holes.Length + 1, allocator);

            int layoutCounter = 0;

            int start = 0;
            int end = iShape.hull.Length - 1;

            int pointCounter = 0;
            for(int k = 0; k < iShape.hull.Length; ++k) {
                this.points[pointCounter++] = iShape.hull[k];
            }

            var layout = new PathLayout(start, iShape.hull.Length, true);
            this.layouts[layoutCounter++] = layout;

            start = end + 1;

            for(int j = 0; j < iShape.holes.Length; ++j) {
                var hole = iShape.holes[j];
                end = start + hole.Length - 1;
                for(int k = 0; k < hole.Length; ++k) {
                    this.points[pointCounter++] = hole[k];
                }

                this.layouts[layoutCounter++] = new PathLayout(start, hole.Length, false);

                start = end + 1;
            }
        }

        // Holes are ignored
        public IntVector DoCentralSymmetry() {
            var c = centralSymmetry(this.Get(0)); 

            for (int i = 0; i < this.points.Length; ++i) {
                var p = this.points[i];
                this.points[i] = new IntVector(p.x - c.x, p.y - c.y); 
            }

            return c;
        }

        public NativeArray<IntVector> Get(int index, Allocator allocator) {
            var layout = this.layouts[index];
            var array = new NativeArray<IntVector>(layout.length, allocator);
            array.Slice(0, layout.length).CopyFrom(this.points.Slice(layout.begin, layout.length));
            return array;
        }
        
        public NativeSlice<IntVector> Get(int index) {
            var layout = this.layouts[index];
            return this.points.Slice(layout.begin, layout.length);
        }
        
        public bool IsClockWise(int index) {
            var layout = layouts[index];
            return this.isClockWise(layout.begin, layout.end);
        }

        public void Dispose() {
            if (this.points.IsCreated) {
                this.points.Dispose();
                this.layouts.Dispose();   
            }
        }

        private static long Area(NativeSlice<IntVector> self) {
            int n = self.Length;
            long sum = 0;
            var p1 = self[n - 1];
            for (int i = 0; i < n; i++) {
                var p2 = self[i];
                long dif_x = p2.x - p1.x;
                long sum_y = p2.y + p1.y;
                sum += dif_x * sum_y;
                p1 = p2;
            }

            return sum >> 1;
        }
        
        private static IntVector centralSymmetry(NativeSlice<IntVector> self) {
            long x = 0;
            long y = 0;
            int n = self.Length;
            var b = self[n - 1];
            for (int i = 0; i < n; ++i) {
                var a = self[i]; 

                long d = a.x * b.y - b.x * a.y;
                x += (a.x + b.x) * d;
                y += (a.y + b.y) * d;

                b = a;
            }

            long k = 6 * Area(self);

            return new IntVector(x / k, y / k);
        }
        
        private bool isClockWise(int begin, int end) {
            long sum = 0;
            var p1 = points[end];
            for (int i = begin; i <= end; i++) {
                var p2 = points[i];
                long dif_x = p2.x - p1.x;
                long sum_y = p2.y + p1.y;
                sum += dif_x * sum_y;
                p1 = p2;
            }

            return sum >= 0;            
        }
    }

}