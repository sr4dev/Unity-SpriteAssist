using Unity.Collections;

namespace iShape.Geometry.Extension {

    public static class IntVector_ArrayExt {
        
        public static long Area(this NativeArray<IntVector> self) {
            int n = self.Length;
            long sum = 0;
            var p1 = self[n - 1];
            for (int i = 0; i < n; i++) {
                var p2 = self[i];
                long x = p2.x - p1.x;
                long y = p2.y + p1.y;
                sum += x * y;
                p1 = p2;
            }

            return sum >> 1;
        }
        
        public static long Area(this NativeSlice<IntVector> self) {
            int n = self.Length;
            long sum = 0;
            var p1 = self[n - 1];
            for (int i = 0; i < n; i++) {
                var p2 = self[i];
                long x = p2.x - p1.x;
                long y = p2.y + p1.y;
                sum += x * y;
                p1 = p2;
            }

            return sum >> 1;
        }
    }

}