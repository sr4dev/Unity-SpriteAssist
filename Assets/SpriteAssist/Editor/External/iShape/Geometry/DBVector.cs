using UnityEngine;

namespace iShape.Geometry {

    public struct DBVector {

        public static readonly DBVector Zero = new DBVector(0, 0);

        public readonly double x;
        public readonly double y;

        public DBVector(double x, double y) {
            this.x = x;
            this.y = y;
        }
        
        public DBVector(IntVector vector) {
            this.x = vector.x;
            this.y = vector.y;
        }
    }

}