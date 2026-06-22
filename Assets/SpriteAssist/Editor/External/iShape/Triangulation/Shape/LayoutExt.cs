using Unity.Collections;
using iShape.Geometry;
using iShape.Collections;
using iShape.Geometry.Container;

namespace iShape.Triangulation.Shape {

    public static class LayoutExt {
        private struct Sub {
            internal Link next; // top branch
            internal Link prev; // bottom branch
            internal readonly bool isEmpty;

            internal Sub(bool isEmpty) {
                this.isEmpty = isEmpty;
                this.next = Link.empty;
                this.prev = Link.empty;
            }

            internal Sub(Link link) {
                this.isEmpty = false;
                this.next = link;
                this.prev = link;
            }

            internal Sub(Link next, Link prev) {
                this.isEmpty = false;
                this.next = next;
                this.prev = prev;
            }
        }

        private readonly struct DualSub {
            internal readonly Link next; // top branch
            internal readonly int middle;
            internal readonly Link prev; // bottom branch

            internal DualSub(Sub nextSub, Sub prevSub) {
                this.next = nextSub.next;
                this.middle = nextSub.prev.self;
                this.prev = prevSub.prev;
            }

            internal DualSub(Link next, int middle, Link prev) {
                this.next = next;
                this.middle = middle;
                this.prev = prev;
            }
        }

        private readonly struct Bridge {
            internal readonly Link a;
            internal readonly Link b;
            internal Slice Slice => new Slice(a.vertex.index, b.vertex.index);

            internal Bridge(Link a, Link b) {
                this.a = a;
                this.b = b;
            }
        }

        public static MonotoneLayout Split(this PlainShape shape, long maxEdge, NativeArray<IntVector> extraPoints, Allocator allocator) {
            var navigator = shape.GetNavigator(maxEdge, extraPoints, Allocator.Temp);
            var links = new DynamicArray<Link>(navigator.links, allocator);
            var natures = navigator.natures;
            var sortIndices = navigator.indices;

            int n = sortIndices.Length;

            var subs = new DynamicArray<Sub>(8, Allocator.Temp);
            var dSubs = new DynamicArray<DualSub>(8, Allocator.Temp);
            var indices = new DynamicArray<int>(16, allocator);
            var slices = new DynamicArray<Slice>(16, allocator);

            int i = 0;

            nextNode:
            while (i < n) {
                int sortIndex = sortIndices[i];
                var node = links[sortIndex];
                var nature = natures[sortIndex];

                int j;
                switch (nature) {

                    case LinkNature.start:
                        subs.Add(new Sub(node));
                        ++i;
                        goto nextNode;
                    case LinkNature.extra:
                        j = 0;

                        while (j < dSubs.Count) {

                            var dSub = dSubs[j];
                            var pA = dSub.next.vertex.point;
                            var pB = links[dSub.next.next].vertex.point;
                            var pC = links[dSub.prev.prev].vertex.point;
                            var pD = dSub.prev.vertex.point;

                            var p = node.vertex.point;

                            if (IsTetragonContain(p, pA, pB, pC, pD)) {
                                var hand = links[dSub.middle];
                                slices.Add(new Slice(hand.vertex.index, node.vertex.index));
                                links.ConnectExtraPrev(hand.self, node.self);

                                dSubs[j] = new DualSub(links[dSub.next.self], node.self, links[dSub.prev.self]);

                                i += 1;
                                goto nextNode;
                            }

                            j += 1;

                        } //  while dSubs

                        j = 0;

                        while (j < subs.Count) {
                            var sub = subs[j];

                            var pA = sub.next.vertex.point;
                            var pB = links[sub.next.next].vertex.point;
                            var pC = links[sub.prev.prev].vertex.point;
                            var pD = sub.prev.vertex.point;

                            var p = node.vertex.point;

                            if (IsTetragonContain(p, pA, pB, pC, pD)) {
                                if (!sub.isEmpty) {
                                    if (pA.x > pD.x) {
                                        var hand = sub.next;
                                        slices.Add(new Slice(hand.vertex.index, node.vertex.index));
                                        var newHandIndex = links.ConnectExtraNext(hand.self, node.self);
                                        dSubs.Add(new DualSub(links[newHandIndex], node.self, links[sub.prev.self]));
                                    } else {
                                        var hand = sub.prev;
                                        slices.Add(new Slice(hand.vertex.index, node.vertex.index));
                                        var newHandIndex = links.ConnectExtraPrev(hand.self, node.self);
                                        dSubs.Add(new DualSub(links[sub.next.self], node.self, links[newHandIndex]));
                                    }
                                } else {
                                    var hand = links[sub.next.self];
                                    slices.Add(new Slice(hand.vertex.index, b: node.vertex.index));
                                    var newPrev = links.ConnectExtraPrev(hand.self, node.self);
                                    dSubs.Add(new DualSub(links[hand.self], node.self, links[newPrev]));
                                }

                                subs.Exclude(j);
                                i += 1;
                                goto nextNode;
                            }

                            j += 1;
                        }

                        break;
                    case LinkNature.merge:

                        var newNextSub = new Sub(true);
                        var newPrevSub = new Sub(true);

                        j = 0;

                        while (j < dSubs.Count) {

                            var dSub = dSubs[j];

                            if (dSub.next.next == node.self) {
                                var a = node.self;
                                var b = dSub.middle;
                                var bridge = links.Connect(a, b);

                                indices.Add(links.FindStart(bridge.a.self));

                                slices.Add(bridge.Slice);

                                var prevSub = new Sub(links[a], dSub.prev);

                                if (!newNextSub.isEmpty) {
                                    dSubs[j] = new DualSub(newNextSub, prevSub);
                                    ++i;
                                    goto nextNode;
                                }

                                dSubs.Exclude(j);

                                newPrevSub = prevSub;
                                continue;
                            } else if (dSub.prev.prev == node.self) {

                                var a = dSub.middle;
                                var b = node.self;

                                var bridge = links.Connect(a, b);

                                indices.Add(links.FindStart(bridge.a.self));
                                slices.Add(bridge.Slice);

                                var nextSub = new Sub(dSub.next, links[b]);

                                if (!newPrevSub.isEmpty) {
                                    dSubs[j] = new DualSub(nextSub, newPrevSub);
                                    ++i;
                                    goto nextNode;
                                }

                                dSubs.Exclude(j);

                                newNextSub = nextSub;
                                continue;
                            }

                            ++j;

                        } //  while dSubs

                        j = 0;

                        while (j < subs.Count) {
                            var sub = subs[j];

                            if (sub.next.next == node.self) {
                                sub.next = node;

                                subs.Exclude(j);

                                if (!newNextSub.isEmpty) {
                                    dSubs.Add(new DualSub(newNextSub, sub));
                                    ++i;
                                    goto nextNode;
                                }

                                newPrevSub = sub;
                                continue;
                            } else if (sub.prev.prev == node.self) {
                                sub.prev = node;
                                subs.Exclude(j);

                                if (!newPrevSub.isEmpty) {
                                    dSubs.Add(new DualSub(sub, newPrevSub));
                                    ++i;
                                    goto nextNode;
                                }

                                newNextSub = sub;
                                continue;
                            }

                            ++j;
                        }

                        break;
                    case LinkNature.split:

                        j = 0;

                        while (j < subs.Count) {
                            var sub = subs[j];


                            var pA = sub.next.vertex.point;

                            var pB = links[sub.next.next].vertex.point;

                            var pC = links[sub.prev.prev].vertex.point;

                            var pD = sub.prev.vertex.point;


                            var p = node.vertex.point;

                            if (IsTetragonContain(p, pA, pB, pC, pD)) {
                                var a0 = sub.next.self;
                                var a1 = sub.prev.self;
                                var b = node.self;

                                if (pA.x > pD.x) {

                                    var bridge = links.Connect(a0, b);

                                    subs.Add(new Sub(bridge.b, links[a1]));

                                    slices.Add(bridge.Slice);
                                } else {
                                    var bridge = links.Connect(a1, b);

                                    subs.Add(new Sub(bridge.b, bridge.a));
                                    slices.Add(bridge.Slice);
                                }

                                subs[j] = new Sub(links[a0], links[b]);
                                ++i;
                                goto nextNode;
                            }

                            ++j;
                        }

                        j = 0;

                        while (j < dSubs.Count) {

                            var dSub = dSubs[j];

                            var pA = dSub.next.vertex.point;

                            var pB = links[dSub.next.next].vertex.point;
                            var pC = links[dSub.prev.prev].vertex.point;
                            var pD = dSub.prev.vertex.point;
                            var p = node.vertex.point;

                            if (IsTetragonContain(p, pA, pB, pC, pD)) {
                                var a = dSub.middle;

                                var b = node.self;
                                var bridge = links.Connect(a, b);

                                subs.Add(new Sub(dSub.next, links[b]));
                                subs.Add(new Sub(bridge.b, dSub.prev));
                                slices.Add(bridge.Slice);
                                dSubs.Exclude(j);

                                ++i;
                                goto nextNode;
                            }

                            ++j;

                        } //  while dSubs

                        break;
                    case LinkNature.end:

                        j = 0;

                        while (j < subs.Count) {
                            var sub = subs[j];

                            // second condition is useless because it repeats the first
                            if (sub.next.next == node.self) /* || sub.prev.prev.index == node.this */ {
                                indices.Add(links.FindStart(node.self));

                                subs.Exclude(j);

                                ++i;
                                goto nextNode;
                            }

                            ++j;
                        }

                        j = 0;

                        while (j < dSubs.Count) {

                            var dSub = dSubs[j];

                            // second condition is useless because it repeats the first
                            if (dSub.next.next == node.self) /*|| dSub.prevSub.prev.prev.index == node.this*/ {
                                var a = dSub.middle;
                                var b = node.self;
                                var bridge = links.Connect(a, b);

                                indices.Add(links.FindStart(a));
                                indices.Add(links.FindStart(bridge.a.self));
                                slices.Add(bridge.Slice);

                                dSubs.Exclude(j);

                                // goto next node
                                ++i;
                                goto nextNode;
                            }

                            ++j;

                        } //  while dSubs

                        break;

                    case LinkNature.simple:

                        j = 0;

                        while (j < subs.Count) {
                            var sub = subs[j];

                            if (sub.next.next == node.self) {
                                sub.next = node;
                                subs[j] = sub;

                                ++i;
                                goto nextNode;
                            } else if (sub.prev.prev == node.self) {
                                sub.prev = node;
                                subs[j] = sub;
                                // goto next node
                                ++i;
                                goto nextNode;
                            }

                            ++j;
                        }

                        j = 0;

                        while (j < dSubs.Count) {

                            var dSub = dSubs[j];

                            if (dSub.next.next == node.self) {

                                var a = dSub.middle;
                                var b = node.self;

                                var bridge = links.Connect(a, b);

                                indices.Add(links.FindStart(node.self));
                                slices.Add(bridge.Slice);

                                var newSub = new Sub(bridge.b, dSub.prev);
                                subs.Add(newSub);

                                dSubs.Exclude(j);

                                // goto next node
                                ++i;
                                goto nextNode;
                            } else if (dSub.prev.prev == node.self) {

                                var a = node.self;
                                var b = dSub.middle;

                                var bridge = links.Connect(a, b);

                                indices.Add(links.FindStart(node.self));
                                slices.Add(bridge.Slice);

                                var newSub = new Sub(links[dSub.next.self], bridge.a);
                                subs.Add(newSub);

                                dSubs.Exclude(j);

                                // goto next node
                                ++i;
                                goto nextNode;
                            }

                            ++j;

                        } //  while dSubs

                        break;
                } // switch
            }

            subs.Dispose();
            dSubs.Dispose();
            navigator.Dispose();

            int pathCount = navigator.pathCount;
            int extraCount = navigator.extraCount;

            return new MonotoneLayout(pathCount, extraCount, links.Convert(), slices.Convert(), indices.Convert());
        }

        private static Bridge Connect(this ref DynamicArray<Link> links, int ai, int bi) {
            var aLink = links[ai];
            var bLink = links[bi];

            var count = links.Count;

            var newLinkA = new Link(aLink.prev, count, count + 1, aLink.vertex);

            links.Add(newLinkA);

            var aPrev = links[aLink.prev];
            aPrev.next = count;
            links[aLink.prev] = aPrev;

            var newLinkB = new Link(count, count + 1, bLink.next, bLink.vertex);

            links.Add(newLinkB);
            var bNext = links[bLink.next];
            bNext.prev = count + 1;
            links[bLink.next] = bNext;


            aLink.prev = bi;
            links[ai] = aLink;

            bLink.next = ai;
            links[bi] = bLink;

            return new Bridge(newLinkA, newLinkB);
        }

        private static int ConnectExtraPrev(this ref DynamicArray<Link> links, int iHand, int iNode) {
            var handLink = links[iHand];
            var nodeLink = links[iNode];
            var iPrev = handLink.prev;
            var prevLink = links[iPrev];

            var count = links.Count;

            var iNewHand = count;

            var newHandLink = new Link(handLink.prev, iNewHand, iNode, handLink.vertex);
            links.Add(newHandLink);

            handLink.prev = iNode;
            nodeLink.next = iHand;
            nodeLink.prev = iNewHand;
            prevLink.next = iNewHand;

            links[iNode] = nodeLink;
            links[iHand] = handLink;
            links[iPrev] = prevLink;

            return iNewHand;
        }

        private static int ConnectExtraNext(this ref DynamicArray<Link> links, int iHand, int iNode) {
            var handLink = links[iHand];
            var nodeLink = links[iNode];
            var iNext = handLink.next;
            var nextLink = links[iNext];

            var count = links.Count;

            var iNewHand = count;

            var newHandLink = new Link(iNode, iNewHand, handLink.next, handLink.vertex);
            links.Add(newHandLink);

            handLink.next = iNode;
            nodeLink.prev = iHand;
            nodeLink.next = iNewHand;
            nextLink.prev = iNewHand;

            links[iNode] = nodeLink;
            links[iHand] = handLink;
            links[iNext] = nextLink;

            return iNewHand;
        }

        private static bool IsTriangleContain(IntVector p, IntVector a, IntVector b, IntVector c) {
            long q0 = (p - b).CrossProduct(a - b);
            long q1 = (p - c).CrossProduct(b - c);
            long q2 = (p - a).CrossProduct(c - a);

            bool has_neg = q0 < 0 || q1 < 0 || q2 < 0;
            bool has_pos = q0 > 0 || q1 > 0 || q2 > 0;

            return !(has_neg && has_pos);
        }

        private static bool IsTriangleNotContain(IntVector p, IntVector a, IntVector b, IntVector c) {
            long q0 = (p - b).CrossProduct(a - b);
            long q1 = (p - c).CrossProduct(b - c);
            long q2 = (p - a).CrossProduct(c - a);

            bool has_neg = q0 <= 0 || q1 <= 0 || q2 <= 0;
            bool has_pos = q0 >= 0 || q1 >= 0 || q2 >= 0;

            return has_neg && has_pos;
        }

        private static bool IsTetragonContain(IntVector p, IntVector a, IntVector b, IntVector c, IntVector d) {
            var ab = a - b;
            var bc = b - c;
            var cd = c - d;
            var da = d - a;

            long da_ab = da.CrossProduct(ab);

            // dab
            if (da_ab > 0) {
                var in_bcd = IsTriangleContain(p,b, c, d);
                var not_dab = IsTriangleNotContain(p,d, a, b);
                return in_bcd && not_dab;
            }

            var ab_bc = ab.CrossProduct(bc);

            // abc
            if (ab_bc > 0) {
                var in_cda = IsTriangleContain(p, c, d, a);
                var not_abc = IsTriangleNotContain(p, a, b, c);
                return in_cda && not_abc;
            }

            var bc_cd = bc.CrossProduct(cd);

            // bcd
            if (bc_cd > 0) {
                var in_dab = IsTriangleContain(p, d, a, b);
                var not_bcd = IsTriangleNotContain(p, b, c, d);
                return in_dab && not_bcd;
            }

            var cd_da = cd.CrossProduct(da);

            // cda
            if (cd_da > 0) {
                var in_abc = IsTriangleContain(p, a, b, c);
                var not_cda = IsTriangleNotContain(p, c, d, a);
                return in_abc && not_cda;
            }

            // convex
            var abc = IsTriangleContain(p, a, b, c);
            var cda = IsTriangleContain(p, c, d, a);
            
            return abc || cda;
        }
    }

    internal static class DynamicArrayExtension {
        internal static void Exclude<T>(this ref DynamicArray<T> array, int index) where T : struct {
            int lastIndex = array.Count - 1;
            if (lastIndex != index) {
                array[index] = array[lastIndex];
            }

            array.RemoveLast();
        }

        internal static int FindStart(this DynamicArray<Link> array, int index) {
            var self = array[index];
            var next = array[self.next];
            var prev = array[self.prev];

            var bit = self.vertex.point.BitPack;
            var aBit = next.vertex.point.BitPack;
            var bBit = prev.vertex.point.BitPack;

            if (aBit < bit) {
                do {
                    next = array[next.next];
                    bit = aBit;
                    aBit = next.vertex.point.BitPack;
                } while (aBit < bit);

                return next.prev;
            } else if (bBit < bit) {
                do {
                    prev = array[prev.prev];
                    bit = bBit;
                    bBit = prev.vertex.point.BitPack;
                } while (bBit < bit);

                return prev.next;
            } else {
                return index;
            }
        }
    }

}