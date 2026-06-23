using iShape.Geometry;

namespace iShape.Triangulation.Shape {

	public struct Link {
		public static readonly Link empty = new Link(0, 0, 0, Vertex.empty);

		public int prev;
		public readonly int self;
		public int next;

		public Vertex vertex;

        public Link(int prev, int self, int next, Vertex vertex) {
            this.prev = prev;
            this.self = self;
            this.next = next;
            this.vertex = vertex;
        }
	}
}