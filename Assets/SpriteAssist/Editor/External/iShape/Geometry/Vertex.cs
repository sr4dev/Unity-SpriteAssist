namespace iShape.Geometry {

    public readonly struct Vertex {

        public enum Nature {
            origin,
            extraPath,
            extraInner,
            extraTessellated
        }

        public static readonly Vertex empty = new Vertex(0, Nature.origin, IntVector.Zero);

        public bool isPath => nature == Nature.origin || nature == Nature.extraPath;

        public readonly int index;
        public readonly Nature nature;
        public readonly IntVector point;

        public Vertex(int index, Nature nature, IntVector point) {
            this.index = index;
            this.nature = nature;
            this.point = point;
        }
    }

}