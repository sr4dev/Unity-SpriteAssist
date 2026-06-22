namespace iShape.Geometry.Polygon {

    public readonly struct Layout {
        
        public readonly Form form;
        public readonly int begin;
        public readonly int length;

        public Layout(int begin, int length, Form form) {
            this.begin = begin;
            this.length = length;
            this.form = form;
        }
    }

}