using Unity.Collections;

namespace iShape.Triangulation.Shape.Delaunay {

    internal struct SliceBuffer {

        private readonly int vertexCount;
        private NativeArray<Side> sides;
        private NativeArray<bool> vertexMark;

        internal SliceBuffer(int vertexCount, NativeArray<Slice> slices, Allocator allocator) {
            this.vertexCount = vertexCount;
            this.vertexMark = new NativeArray<bool>(vertexCount, allocator);
            int n = slices.Length;
            this.sides = new NativeArray<Side>(n, allocator);

            for(int i = 0; i < n; ++i) {
                var slice = slices[i];

                vertexMark[slice.a] = true;
                vertexMark[slice.b] = true;

                int id;

                if(slice.a < slice.b) {
                    id = slice.a * vertexCount + slice.b;
                } else {
                    id = slice.b * vertexCount + slice.a;
                }
                sides[i] = new Side(id, -1, -1);
            }

            Sort(sides);
        }

        public void Dispose() {
            this.sides.Dispose();
            this.vertexMark.Dispose();
        }

        public void AddConnections(NativeArray<Triangle> triangles) {
            int n = triangles.Length;

            for(int i = 0; i < n; ++i) {
                var triangle = triangles[i];
                int a = triangle.vA.index;
                int b = triangle.vB.index;
                int c = triangle.vC.index;

                int sideIndex = this.Find(a, b);
                if(sideIndex >= 0) {
                    var side = this.sides[sideIndex];
                    if(side.IsEmpty) {
                        side.triangle = i;
                        side.edge = 2;
                        this.sides[sideIndex] = side;
                    } else {
                        triangle.SetNeighbor(2, side.triangle);
                        var neighbor = triangles[side.triangle];
                        neighbor.SetNeighbor(side.edge, i);
                        triangles[side.triangle] = neighbor;
                        triangles[i] = triangle;
                    }
                }

                sideIndex = this.Find(a, c);
                if(sideIndex >= 0) {
                    var side = this.sides[sideIndex];
                    if(side.IsEmpty) {
                        side.triangle = i;
                        side.edge = 1;
                        this.sides[sideIndex] = side;
                    } else {
                        triangle.SetNeighbor(1, side.triangle);
                        var neighbor = triangles[side.triangle];
                        neighbor.SetNeighbor(side.edge, i);
                        triangles[side.triangle] = neighbor;
                        triangles[i] = triangle;
                    }
                }

                sideIndex = this.Find(b, c);
                if(sideIndex >= 0) {
                    var side = this.sides[sideIndex];
                    if(side.IsEmpty) {
                        side.triangle = i;
                        side.edge = 0;
                        this.sides[sideIndex] = side;
                    } else {
                        triangle.SetNeighbor(0, side.triangle);
                        var neighbor = triangles[side.triangle];
                        neighbor.SetNeighbor(side.edge, i);
                        triangles[side.triangle] = neighbor;
                        triangles[i] = triangle;
                    }
                }
            }
        }

        private int Find(int a, int b) {
            if(!vertexMark[a] || !vertexMark[b]) {
                return -1;
            }
            int id;
            if(a < b) {
                id = a * vertexCount + b;
            } else {
                id = b * vertexCount + a;
            }

            var left = 0;
            var right = sides.Length - 1;

            do {
                int k;
                if(left + 1 < right) {
                    k = (left + right) >> 1;
                } else {
                    do {
                        if(sides[left].id == id) {
                            return left;
                        }
                        ++left;
                    } while(left <= right);
                    return -1;
                }

                int e = sides[k].id;
                if(e > id) {
                    right = k;
                } else if(e < id) {
                    left = k;
                } else {
                    return k;
                }
            } while(true);
        }

        private static void Sort(NativeArray<Side> array) {
            int n = array.Length;
            int r = 2;
            int rank = 1;

            while(r <= n) {
                rank = r;
                r <<= 1;
            }
            rank -= 1;

            int jEnd = rank;

            int jStart = ((jEnd + 1) >> 1) - 1;


            while(jStart >= 0) {
                int k = jStart;
                while(k < jEnd) {
                    int j = k;

                    var a = array[j];
                    bool fallDown;
                    do {
                        fallDown = false;

                        int j0 = (j << 1) + 1;
                        int j1 = j0 + 1;

                        if(j1 < n) {
                            var a0 = array[j0];
                            var a1 = array[j1];

                            if(a.id < a0.id || a.id < a1.id) {
                                if(a0.id > a1.id) {
                                    array[j0] = a;
                                    array[j] = a0;
                                    j = j0;
                                } else {
                                    array[j1] = a;
                                    array[j] = a1;
                                    j = j1;
                                }
                                fallDown = j < rank;
                            }
                        } else if(j0 < n) {
                            var ax = array[j];
                            var a0 = array[j0];
                            if(ax.id < a0.id) {
                                array[j0] = ax;
                                array[j] = a0;
                            }
                        }

                    } while(fallDown);
                    ++k;
                }

                jEnd = jStart;
                jStart = ((jEnd + 1) >> 1) - 1;
            }

            while(n > 0) {
                int m = n - 1;

                var a = array[m];
                array[m] = array[0];
                array[0] = a;

                int j = 0;
                bool fallDown;
                do {
                    fallDown = false;

                    int j0 = (j << 1) + 1;
                    int j1 = j0 + 1;

                    if(j1 < m) {
                        var a0 = array[j0];
                        var a1 = array[j1];
                        fallDown = a.id < a0.id || a.id < a1.id;

                        if(fallDown) {
                            if(a0.id > a1.id) {
                                array[j0] = a;
                                array[j] = a0;
                                j = j0;
                            } else {
                                array[j1] = a;
                                array[j] = a1;
                                j = j1;
                            }
                        }
                    } else if(j0 < m) {
                        var ax = array[j];
                        var a0 = array[j0];
                        if(ax.id < a0.id) {
                            array[j0] = ax;
                            array[j] = a0;
                        }
                    }

                } while(fallDown);

                n = m;
            }
        }
    }
}
