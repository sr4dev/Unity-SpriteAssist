using iShape.Geometry.Container;

namespace iShape.Geometry {

	public struct IntShape {

        public static readonly IntShape empty = new IntShape(null, null);
        
        public readonly IntVector[] hull;
        public readonly IntVector[][] holes;
        
        public IntShape(Shape shape, IntGeom iGeom) {
            this.hull = iGeom.Int(shape.hull);
            this.holes = iGeom.Int(shape.holes);
        }

        public IntShape(PlainShape plainShape) {
            var hullLayout = plainShape.layouts[0];
            this.hull = new IntVector[hullLayout.end - 1];
            for(int i = 0; i < hullLayout.end; ++i) {
                this.hull[i] = plainShape.points[i];
            }

            int holesCount = plainShape.layouts.Length - 1;
            this.holes = new IntVector[holesCount][];
            for(int i = 0; i < holesCount; ++i) {
                var layout = plainShape.layouts[i + 1];
                int n = layout.end - layout.begin + 1;
                var points = new IntVector[n];
                for(int j = 0; j < n; ++j) {
                    points[j] = plainShape.points[j + layout.begin];
                }
                this.holes[i] = points;
            }
        }

        public IntShape(IntVector[] hull, IntVector[][] holes) {
            this.hull = hull;
            this.holes = holes;
        }

    }

}