using iShape.Collections;
using Unity.Collections;

namespace iShape.Geometry.Container {

    public struct DynamicPlainShape {
        
        public DynamicArray<IntVector> points;
        public DynamicArray<PathLayout> layouts;

        public DynamicPlainShape(Allocator allocator) {
            this.points = new DynamicArray<IntVector>(allocator);
            this.layouts = new DynamicArray<PathLayout>(allocator);
        }

        public DynamicPlainShape(PlainShape plainShape, Allocator allocator) {
            this.points = new DynamicArray<IntVector>(plainShape.points, allocator);
            this.layouts = new DynamicArray<PathLayout>(plainShape.layouts, allocator);
        }

        public DynamicPlainShape(NativeArray<IntVector> points, NativeArray<PathLayout> layouts) {
            this.points = new DynamicArray<IntVector>(points);
            this.layouts = new DynamicArray<PathLayout>(layouts);
        }
        
        public DynamicPlainShape(NativeArray<IntVector> points, bool isClockWise, Allocator allocator) {
            this.points = new DynamicArray<IntVector>(points, allocator);
            this.layouts = new DynamicArray<PathLayout>(1, allocator);
            this.layouts.Add(new PathLayout(0, points.Length, isClockWise));
        }
        
        public DynamicPlainShape(NativeSlice<IntVector> points, bool isClockWise, Allocator allocator) {
            this.points = new DynamicArray<IntVector>(points, allocator);
            this.layouts = new DynamicArray<PathLayout>(1, allocator);
            this.layouts.Add(new PathLayout(0, points.Length, isClockWise));
        }
        
        public DynamicPlainShape(int pointsCapacity, int layoutsCapacity, Allocator allocator) {
            this.points = new DynamicArray<IntVector>(pointsCapacity, allocator);
            this.layouts = new DynamicArray<PathLayout>(layoutsCapacity, allocator);
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

        public void Add(NativeArray<IntVector> path, bool isClockWise) {
            int begin = points.Count;
            var layout = new PathLayout(begin, path.Length, isClockWise);
            this.points.Add(path);
            this.layouts.Add(layout);
        }

        public void Add(NativeSlice<IntVector> path, bool isClockWise) {
            int begin = points.Count;
            var layout = new PathLayout(begin, path.Length, isClockWise);
            this.points.Add(path);
            this.layouts.Add(layout);
        }
        
        public void Add(PlainShape shape) {
            // TODO optimise
            int n = shape.Count;
            for (int i = 0; i < n; ++i) {
                this.Add(shape.Get(i), shape.layouts[i].isClockWise);
            }
        }
        
        public void AddEmpty() {
            int begin = points.Count;
            this.layouts.Add(new PathLayout(begin, 0, true));            
        }
        
        public void RemoveAt(int index) {
            int count = this.layouts.Count;
            if (index + 1 == count) {
                this.layouts.RemoveLast();
                return;
            }

            var layout = this.layouts[index];
            var lastLayout = this.layouts[count - 1];
            this.layouts.RemoveAt(index);

            int tailStart = layout.begin + layout.length;
            int length = lastLayout.begin + lastLayout.length - tailStart;
            var tailSlice = this.points.Slice(tailStart, length);
            this.points.Slice(layout.begin, length).CopyFrom(tailSlice);
        }

        public void ReplaceAt(int index, NativeSlice<IntVector> path) {
            var oldLayout = this.layouts[index];
            var newLayout = new PathLayout(oldLayout.begin, path.Length, oldLayout.isClockWise);
            if (newLayout.length == oldLayout.length) {
                this.points.Slice(oldLayout.begin, oldLayout.length).CopyFrom(path);
            } else if (index + 1 == this.layouts.Count) {
                this.layouts[index] = newLayout;
                this.points.RemoveLast(oldLayout.length);
                this.points.Add(path);
            } else {
                this.layouts[index] = newLayout;
                int shift = newLayout.length - oldLayout.length;
                int oldTailLength = this.points.Count - oldLayout.end - 1;
                this.points.Shift(shift);
                
                this.points.Slice(newLayout.end + 1, oldTailLength).CopyFrom(this.points.Slice(oldLayout.end + 1, oldTailLength));
                this.points.Slice(newLayout.begin, newLayout.length).CopyFrom(path);
                
                for(int i = index + 1; i < this.layouts.Count; ++i) {
                    var lt = this.layouts[i];
                    this.layouts[i] = new PathLayout(lt.begin + shift, lt.length, lt.isClockWise);
                }
            }
        }

        public PlainShape Convert() {
            return new PlainShape(this.points.Convert(), this.layouts.Convert());
        }

        public void Dispose() {
            this.points.Dispose();
            this.layouts.Dispose();
        }
    }

}