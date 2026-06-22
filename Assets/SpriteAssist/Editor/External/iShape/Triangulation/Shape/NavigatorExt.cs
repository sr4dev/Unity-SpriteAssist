using iShape.Collections;
using Unity.Collections;
using iShape.Geometry;
using iShape.Geometry.Container;
using UnityEngine;

namespace iShape.Triangulation.Shape {

    internal static class PlainShapeNavigatorExt {
	    private struct Node {
		    internal readonly int index;
		    internal IntVector point;

		    internal Node(int index, IntVector point) {
			    this.index = index;
			    this.point = point;
		    }
	    }

	    private struct SplitLayout {
		    internal NativeArray<PathLayout> layouts;
		    internal NativeArray<Node> nodes;
		    
		    internal SplitLayout(NativeArray<PathLayout> layouts, NativeArray<Node> nodes) {
			    this.layouts = layouts;
			    this.nodes = nodes;
		    }

		    internal void Dispose() {
			    this.layouts.Dispose();
			    this.nodes.Dispose();
		    }
	    }
	    
		private readonly struct SortData {
			internal readonly int index;
			internal readonly long factor;
			private readonly int nature;

			internal SortData(int index, long factor, int nature) {
				this.index = index;
				this.factor = factor;
				this.nature = nature;
			}

			public static bool operator <(SortData a, SortData b) {
				if(a.factor != b.factor) {
					return a.factor < b.factor;
				} else if (a.nature != b.nature) {
                    return a.nature < b.nature;
                } else {
                    return a.index < b.index;
                }
			}

			public static bool operator >(SortData a, SortData b) {
				if(a.factor != b.factor) {
					return a.factor > b.factor;
                } else if(a.nature != b.nature) {
                    return a.nature > b.nature;
                } else {
                    return a.index > b.index;
                }
            }
		}

		internal static ShapeNavigator GetNavigator(this PlainShape shape, long maxEdge, NativeArray<IntVector> extraPoints, Allocator allocator) {
			SplitLayout splitLayout;
			if (maxEdge == 0) {
				splitLayout = shape.Plain(Allocator.Temp);
			} else {
				splitLayout = shape.Split(maxEdge, Allocator.Temp);
			}
			
			int pathCount = splitLayout.nodes.Length;
			int extraCount = extraPoints.Length;
			
			int n;
            if (extraCount > 0) {
	            n = pathCount + extraCount;
            } else {
	            n = pathCount;
            }

            var links = new NativeArray<Link>(n, allocator);
            var natures = new NativeArray<LinkNature>(n, allocator);

            int m = splitLayout.layouts.Length;
            for(int j = 0; j < m; ++j) {
                var layout = splitLayout.layouts[j];
                var prev = layout.end - 1;

                var self = layout.end;
                var next = layout.begin;

                var a = splitLayout.nodes[prev];
                var b = splitLayout.nodes[self];

                var A = a.point.BitPack;
                var B = b.point.BitPack;

                while(next <= layout.end) {
                    var c = splitLayout.nodes[next];
                    var C = c.point.BitPack;

                    var nature = LinkNature.simple;
                    bool isCCW = IsCCW(a.point, b.point, c.point);

                    if(layout.isClockWise) {
	                    if(A > B && B < C) {
		                    if(isCCW) {
			                    nature = LinkNature.start;
		                    } else {
			                    nature = LinkNature.split;
		                    }
	                    }

	                    if(A < B && B > C) {
		                    if(isCCW) {
			                    nature = LinkNature.end;
		                    } else {
			                    nature = LinkNature.merge;

		                    }
	                    }
                    } else {
	                    if(A > B && B < C) {
		                    if(isCCW) {
			                    nature = LinkNature.start;
		                    } else {
			                    nature = LinkNature.split;
		                    }
	                    }

	                    if(A < B && B > C) {
		                    if(isCCW) {
			                    nature = LinkNature.end;
		                    } else {
			                    nature = LinkNature.merge;
		                    }
	                    }
                    }

                    var verNature = b.index < shape.points.Length ? Vertex.Nature.origin : Vertex.Nature.extraPath;

                    links[self] = new Link(prev, self, next, new Vertex(self, verNature, b.point));
                    natures[self] = nature;

                    a = b;
                    b = c;

                    A = B;
                    B = C;

                    prev = self;
                    self = next;

                    ++next;
                }
            }

            splitLayout.Dispose();

            if (extraCount > 0) {
	            for(int k = 0; k < extraPoints.Length; ++k) {
		            var p = extraPoints[k];
		            var j = k + pathCount;
		            links[j] = new Link(j, j, j, new Vertex(j, Vertex.Nature.extraInner, p));
		            natures[j] = LinkNature.extra;
	            }
            }
            
            // sort
            
			var dataList = new NativeArray<SortData>(n, Allocator.Temp);

			for(int j = 0; j < n; ++j) {
				var p = links[j].vertex.point;
				dataList[j] = new SortData(j, p.BitPack, (int)natures[j]);
			}

			Sort(dataList);

			var indices = new NativeArray<int>(n, allocator);

			// filter same points
			var x1 = new SortData(-1, long.MinValue, int.MinValue);

			int i = 0;

			while(i < n) {
				var x0 = dataList[i];
				indices[i] = x0.index;
				if(x0.factor == x1.factor) {
					var v = links[x1.index].vertex;

					do {
						var link = links[x0.index];
						links[x0.index] = new Link(link.prev, link.self, link.next, new Vertex(v.index, v.nature, v.point));
						++i;
						if(i < n) {
							x0 = dataList[i];
							indices[i] = x0.index;
						} else {
							break;
						}
					} while(x0.factor == x1.factor);
				}
				x1 = x0;
				++i;
			}

			dataList.Dispose();

			return new ShapeNavigator(pathCount, extraCount, links, natures, indices);
        }

		private static void Sort(NativeArray<SortData> array) {
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

							if(a < a0 || a < a1) {
								if(a0 > a1) {
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
							if(ax < a0) {
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
						fallDown = a < a0 || a < a1;

						if(fallDown) {
							if(a0 > a1) {
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
						if(ax < a0) {
							array[j0] = ax;
							array[j] = a0;
						}
					}

				} while(fallDown);

				n = m;
			}
		}

		private static bool IsCCW(IntVector a, IntVector b, IntVector c) {
            long m0 = (c.y - a.y) * (b.x - a.x);
            long m1 = (b.y - a.y) * (c.x - a.x);

            return m0 < m1;
        }
		
	    private static SplitLayout Split(this PlainShape self, long maxEgeSize, Allocator allocator) {
		    var originalCount = self.points.Length;
		    var nodes = new DynamicArray<Node>(originalCount, allocator);
		    var layouts = new DynamicArray<PathLayout>(originalCount, allocator);
		    var sqrMaxSize = maxEgeSize * maxEgeSize;

		    var begin = 0;
		    var originalIndex = 0;
		    var extraIndex = originalCount;

		    for (int j = 0; j < self.layouts.Length; ++j) {
			    var layout = self.layouts[j];
				var last = layout.end;
				var a = self.points[last];
				var length = 0;
            
				for (int i = layout.begin; i <= layout.end; ++i) {
					var b = self.points[i];
					var dx = b.x - a.x;
					var dy = b.y - a.y;
					var sqrSize = dx * dx + dy * dy;
					if (sqrSize > sqrMaxSize) {
						var l = (long) Mathf.Sqrt(sqrSize);
						int s = (int) (l / maxEgeSize);
						double ds = s;
						double sx = dx / ds;
						double sy = dy / ds;
						double fx = 0;
						double fy = 0;
						for (int k = 1; k < s; ++k) {
							fx += sx;
							fy += sy;

							long x = a.x + (long) fx;
							long y = a.y + (long) fy;
							nodes.Add(new Node(extraIndex, new IntVector(x, y)));
							extraIndex += 1;
						}

						length += s - 1;
					}

					length += 1;
					nodes.Add(new Node(originalIndex, b));
					originalIndex += 1;
					a = b;
				}

				layouts.Add(new PathLayout(begin, length, layout.isClockWise));
				begin += length;
		    }

		    return new SplitLayout(layouts.Convert(), nodes.Convert());
	    }
    
	    private static SplitLayout Plain(this PlainShape self, Allocator allocator) {
		    var nodes = new NativeArray<Node>(self.points.Length, allocator);
		    for (int i = 0; i < self.points.Length; ++i) {
			    nodes[i] = new Node(i, self.points[i]);
		    }

		    var layouts = new NativeArray<PathLayout>(self.layouts, allocator);

		    return new SplitLayout(layouts, nodes);
	    }
    }

}