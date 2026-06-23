using iShape.Geometry;

namespace iShape.Triangulation.Shape.Delaunay {

    public struct Triangle {

        internal readonly int index;

        // a(0), b(1), c(2)
        internal Vertex vA;
        internal Vertex vB;
        internal Vertex vC;

        // BC - a(0), AC - b(1), AB - c(2)
        internal int nA;
        internal int nB;
        internal int nC;

        internal Triangle(int index, Vertex a, Vertex b, Vertex c) {
            this.index = index;
            this.vA = a;
            this.vB = b;
            this.vC = c;

            this.nA = -1;
            this.nB = -1;
            this.nC = -1;
        }
        
        internal Triangle(int index, Vertex a, Vertex b, Vertex c, int nA, int nB, int nC) {
            this.index = index;
            this.vA = a;
            this.vB = b;
            this.vC = c;

            this.nA = nA;
            this.nB = nB;
            this.nC = nC;
        }
        
        internal Vertex Vertex(int i) {
            switch(i) {
                case 0:
                    return vA;
                case 1:
                    return vB;
                default:
                    return vC;
            }
        }
        
        internal int FindIndex(int vertexIndex) {
            if(vA.index == vertexIndex) {
                return 0;
            }

            return vB.index == vertexIndex ? 1 : 2;
        }

        internal int Opposite(int neighbor) {
            if(nA == neighbor) {
                return 0;
            }

            return nB == neighbor ? 1 : 2;
        }
        
        internal Vertex OppositeVertex(int neighbor) {
            if(nA == neighbor) {
                return vA;
            }

            return nB == neighbor ? vB : vC;
        }

        internal int Neighbor(int i) {
            switch(i) {
                case 0:
                    return nA;
                case 1:
                    return nB;
                default:
                    return nC;
            }
        }

        internal int FindNeighbor(int vertexIndex) {
            if(vA.index == vertexIndex) {
                return nA;
            }

            return vB.index == vertexIndex ? nB : nC;
        }

        internal void SetNeighbor(int i, int value) {
            switch(i) {
                case 0:
                    nA = value;
                    break;
                case 1:
                    nB = value;
                    break;
                default:
                    nC = value;
                    break;
            }
        }
        
        internal void SetVertex(int i, Vertex value) {
            switch(i) {
                case 0:
                    vA = value;
                    break;
                case 1:
                    vB = value;
                    break;
                default:
                    vC = value;
                    break;
            }
        }
        
        internal void Update(Vertex vertex) {
            if (vA.index != vertex.index) {
                vA = vertex;
            } else if (vB.index != vertex.index) {
                vB = vertex;
            } else if (vC.index != vertex.index) {
                vC = vertex;
            }
        }

        internal void UpdateOpposite(int oldNeighbor, int newNeighbor) {
            if(nA == oldNeighbor) {
                nA = newNeighbor;
				return;
            }

            if(nB == oldNeighbor) {
                nB = newNeighbor;
				return;
			}

            if(nC == oldNeighbor) {
                nC = newNeighbor;
            }
        }
        
        internal int AdjacentNeighbor(int vertex, int neighbor) {
            if (vA.index != vertex && nA != neighbor) {
                return nA;
            }
            if (vB.index != vertex && nB != neighbor) {
                return nB;
            }
            return nC;
        }
    }

}
