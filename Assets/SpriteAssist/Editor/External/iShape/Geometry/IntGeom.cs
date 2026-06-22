using Unity.Collections;
using UnityEngine;

namespace iShape.Geometry {

    public readonly struct IntGeom {

        public static readonly IntGeom DefGeom = new IntGeom(10000);

        public const int maxBits = 31;
        private readonly float scale;
        private readonly float invertScale;

        public float MaxValue => Mathf.Exp(Mathf.Log(2) * maxBits) * invertScale;

        public IntGeom(float scale = 10000) {
            this.scale = scale;
            this.invertScale = 1 / scale;
        }

        public long Int(float value) {
             return (long)Mathf.Round(value * scale);
        }
        
        public long SqrInt(float value) {
            return (long) Mathf.Round(value * scale * scale);
        }

        public IntVector Int(Vector2 vector) {
            return new IntVector((long)Mathf.Round(vector.x * scale), (long)Mathf.Round(vector.y * scale));
        }

        public NativeArray<IntVector> Int(NativeArray<Vector2> points, Allocator allocator) {
            int n = points.Length;
            var array = new NativeArray<IntVector>(n, allocator);
            int i = 0;
            while(i < n) {
                var point = points[i];
                array[i] = new IntVector((long)Mathf.Round(point.x * scale), (long)Mathf.Round(point.y * scale));
                i += 1;
            }
            return array;
        }

        public NativeArray<IntVector> Int(Vector2[] points, Allocator allocator) {
            int n = points.Length;
            var array = new NativeArray<IntVector>(n, allocator);
            int i = 0;
            while(i < n) {
                var point = points[i];
                array[i] = new IntVector((long)Mathf.Round(point.x * scale), (long)Mathf.Round(point.y * scale));
                i += 1;
            }
            return array;
        }
        
        public IntVector[] Int(Vector2[] points) {
            int n = points.Length;
            var array = new IntVector[n];
            int i = 0;
            while(i < n) {
                var point = points[i];
                array[i] = new IntVector((long)Mathf.Round(point.x * scale), (long)Mathf.Round(point.y * scale));
                i += 1;
            }
            return array;
        }

        public IntVector[][] Int(Vector2[][] paths) {
            int n = paths.Length;
			var list = new IntVector[n][];
			for(int i = 0; i < n; ++i) {
				list[i] = this.Int(paths[i]);
			}
            return list;
        }

        public float Float(long value) {
            return value * invertScale;
        }
        
        public float SqrFloat(long value) {
            return value * invertScale * invertScale;
        }

        public Vector2 Float(IntVector point) {
            return new Vector2(point.x * invertScale, point.y * invertScale);
        }

        public NativeArray<Vector2> Float(NativeArray<IntVector> points, Allocator allocator) {
            int n = points.Length;
            var array = new NativeArray<Vector2>(n, allocator);
            int i = 0;
            while(i < n) {
                var point = points[i];
                array[i] = new Vector2(point.x * invertScale, point.y * invertScale);
                i += 1;
            }
            return array;
        }

        public NativeArray<Vector2> Float(NativeSlice<IntVector> points, Allocator allocator) {
            int n = points.Length;
            var array = new NativeArray<Vector2>(n, allocator);
            int i = 0;
            while(i < n) {
                var point = points[i];
                array[i] = new Vector2(point.x * invertScale, point.y * invertScale);
                i += 1;
            }
            return array;
        }

        public Vector2[] Float(IntVector[] points) {
            int n = points.Length;
            var array = new Vector2[n];
            int i = 0;
            while(i < n) {
                var point = points[i];
                array[i] = new Vector2(point.x * invertScale, point.y * invertScale);
                i += 1;
            }
            return array;
        }

        public Vector2[][] Float(IntVector[][] paths) {
            int n = paths.Length;
            var list = new Vector2[n][];
			for(int i = 0; i < n; ++i) {
				list[i++] = this.Float(paths[i]);
			}
            return list;
        }
    }

}