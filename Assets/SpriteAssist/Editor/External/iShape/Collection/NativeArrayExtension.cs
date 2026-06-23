using Unity.Collections;

namespace iShape.Collections {
    
    public static class NativeArrayExtension {

        internal static void Copy<T>(this NativeArray<T> dest, int destIdx, NativeArray<T> src, int srcIdx, int count) where T : struct {
            dest.Slice(destIdx, count).CopyFrom(src.Slice(srcIdx, count));
        }

        internal static void Copy<T>(this NativeArray<T> dest, int destIdx, NativeSlice<T> src, int srcIdx, int count) where T : struct {
            dest.Slice(destIdx, count).CopyFrom(src.Slice(srcIdx, count));
        }

        internal static int Copy<T>(this NativeArray<T> dest, NativeArray<T> src, int offset) where T : struct {
            int length = src.Length;
            dest.Slice(offset, length).CopyFrom(src);
            return length + offset;
        }

        internal static int Copy<T>(this NativeArray<T> dest, NativeSlice<T> src, int offset) where T : struct {
            int length = src.Length;
            dest.Slice(offset, length).CopyFrom(src);
            return length + offset;
        }

        public static NativeArray<T> Reversed<T>(this NativeSlice<T> slice, Allocator allocator) where T : struct {
            int length = slice.Length;
            var array = new NativeArray<T>(length, allocator);
            array.Slice(0, length).CopyFrom(slice);
            int n = length >> 1;
            for (int i = 0, j = length - 1; i < n; ++i, --j) {
                var a = array[i];
                var b = array[j];
                array[j] = a;
                array[i] = b;
            }

            return array;
        }
        
        public static NativeArray<T> Reverse<T>(this NativeArray<T> self) where T : struct {
            int length = self.Length;
            int n = self.Length >> 1;
            for (int i = 0, j = length - 1; i < n; ++i, --j) {
                var a = self[i];
                var b = self[j];
                self[j] = a;
                self[i] = b;
            }

            return self;
        }
        
        public static T[] Convert<T>(this NativeArray<T> self) where T : struct {
            var array = self.ToArray();
            self.Dispose();
            return array;
        }

    }
}