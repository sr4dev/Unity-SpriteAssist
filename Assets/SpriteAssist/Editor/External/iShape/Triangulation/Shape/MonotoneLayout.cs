using Unity.Collections;

namespace iShape.Triangulation.Shape {

    public struct MonotoneLayout {

	    public readonly int pathCount;
	    public readonly int extraCount;
		public NativeArray<Link> links;
		public NativeArray<Slice> slices;
		public NativeArray<int> indices;

        public MonotoneLayout(int pathCount, int extraCount, NativeArray<Link> links, NativeArray<Slice> slices, NativeArray<int> indices) {
	        this.pathCount = pathCount;
	        this.extraCount = extraCount;
	        this.links = links;
            this.slices = slices;
            this.indices = indices;
        }

        public void Dispose() {
        	links.Dispose();
        	slices.Dispose();
        	indices.Dispose();
        }
    }
}