using Unity.Collections;

namespace iShape.Triangulation.Shape {

    internal struct ShapeNavigator {

	    internal readonly int pathCount;
	    internal readonly int extraCount;
        internal NativeArray<Link> links;
        internal NativeArray<LinkNature> natures;
		internal NativeArray<int> indices;

		internal ShapeNavigator(int pathCount, int extraCount, NativeArray<Link> links, NativeArray<LinkNature> natures, NativeArray<int> indices) {
			this.pathCount = pathCount;
			this.extraCount = extraCount;
			this.links = links;
            this.natures = natures;
			this.indices = indices;
		}

        internal void Dispose() {
	        this.links.Dispose();
            this.natures.Dispose();
			this.indices.Dispose();
		}
    }
}