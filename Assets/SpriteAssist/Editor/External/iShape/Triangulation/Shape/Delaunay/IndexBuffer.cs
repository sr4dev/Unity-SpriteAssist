using iShape.Collections;
using Unity.Collections;

namespace iShape.Triangulation.Shape.Delaunay {
    internal struct IndexBuffer {
        
        private struct Link {
            internal static readonly Link empty = new Link(true, -1);
            internal readonly bool isFree;
            internal int next;

            internal Link(bool isFree, int next) {
                this.isFree = isFree;
                this.next = next;
            }
        }
        
        private DynamicArray<Link> array;
        private int first;
        internal IndexBuffer(int count, Allocator allocator) {
            this.array = new DynamicArray<Link>(count, count, allocator);
            if (count == 0) {
                this.first = -1;    
            }
            this.first = 0;
            for (int i = 0; i < count - 1; ++i) {
                this.array[i] = new Link(false, i + 1);
            }

            this.array[count - 1] = new Link(false, -1);
        }
        
        internal bool hasNext => first >= 0;

        internal int Next() {
            int index = first;
            first = array[index].next;
            array[index] = Link.empty;
            return index;
        }

        internal void Add(int index) {
            var isOverflow = index >= array.Count;
            if (isOverflow || array[index].isFree) {
                if (isOverflow) {
                    int count = index - array.Count + 1;
                    array.Add(Link.empty, count);
                }

                array[index] = new Link(false, first);
                if (first >= 0) {
                    var link = array[first];
                    array[first] = link;
                }

                first = index;
            }
        }

        internal void Dispose() {
            this.array.Dispose();
        }
    }

}