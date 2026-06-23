using UnityEngine;

namespace iShape.Geometry {

    [System.Serializable]
    public struct IntVector {

        public static readonly IntVector Zero = new IntVector(0, 0);

        public long x;
        public long y;

        public long magnitude => x * x + y * y;
        
        public long BitPack => (x << IntGeom.maxBits) + y;

        public IntVector(long x, long y) {
            this.x = x;
            this.y = y;
        }

        public static IntVector operator+ (IntVector left, IntVector right) {
            return new IntVector(left.x + right.x, left.y + right.y);
        }
        
        public static IntVector operator- (IntVector left, IntVector right) {
            return new IntVector(left.x - right.x, left.y - right.y);
        }

        public static bool operator== (IntVector left, IntVector right) {
            return left.x == right.x && left.y == right.y;
        }
        
        public static bool operator!= (IntVector left, IntVector right) {
            return left.x != right.x || left.y != right.y;
        }

        private bool Equals(IntVector other) {
            return x == other.x && y == other.y;
        }

        public override bool Equals(object obj) {
            return obj is IntVector other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                return (int)(x * 397 + y);
            }
        }

        public long ScalarMultiply(IntVector vector) {
            return this.x * vector.x + vector.y * this.y;
        }   
        
        public long CrossProduct(IntVector vector) {
            return this.x * vector.y - this.y * vector.x;
        }
    
        public IntVector Normal(IntGeom iGeom) {
            var p = iGeom.Float(this);
            var l = Mathf.Sqrt(p.x * p.x + p.y * p.y);
            float k = 1.0f / l;

            return iGeom.Int(new Vector2(k * p.x, k * p.y));
        }
    
        public readonly long SqrDistance(IntVector vector) {
            var dx = vector.x - this.x;
            var dy = vector.y - this.y;

            return dx * dx + dy * dy;
        }

        public override string ToString() {
            float fx = IntGeom.DefGeom.Float(x);
            float fy = IntGeom.DefGeom.Float(y);
            return $"{x}, {y} ({fx}, {fy})";
        }
    }

}