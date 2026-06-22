namespace iShape.Triangulation.Shape.Delaunay {

    internal struct Side {

        internal readonly int id;
        internal int edge;
        internal int triangle;

        internal bool IsEmpty => triangle == -1;

        internal Side(int id, int edge, int triangle) {
            this.id = id;
            this.edge = edge;
            this.triangle = triangle;
        }
    }
}