using iShape.Geometry;

namespace iShape.Triangulation.Util {

	public struct IntTriangle {

		public enum Orientation {
            clockWise, counterClockWise, line
        }

        public static Orientation GetOrientation(IntVector a, IntVector b, IntVector c) {
            long m0 = (c.y - a.y) * (b.x - a.x);
            long m1 = (b.y - a.y) * (c.x - a.x);

            if(m0 < m1) {
                return Orientation.clockWise;
            } else if(m0 > m1) {
                return Orientation.counterClockWise;
            } else {
                return Orientation.line;
            }
        }

		public static bool IsNotLine(IntVector a, IntVector b, IntVector c) {
			long m0 = (c.y - a.y) * (b.x - a.x);
			long m1 = (b.y - a.y) * (c.x - a.x);

			return m0 != m1;
		}

        public static bool IsCCW_or_Line(IntVector a, IntVector b, IntVector c) {
            long m0 = (c.y - a.y) * (b.x - a.x);
            long m1 = (b.y - a.y) * (c.x - a.x);

            return m0 <= m1;
        }
    }
}