using System.Collections.Generic;
using UnityEngine;

namespace iShape.Geometry {

	public readonly struct Shape {

		public readonly Vector2[] hull;

		public readonly Vector2[][] holes;

        public Shape(IntShape shape) : this(shape, IntGeom.DefGeom) { }

        public Shape(IntShape shape, IntGeom iGeom) {
			this.hull = iGeom.Float(shape.hull);
			this.holes = iGeom.Float(shape.holes);
        }

        public Shape(Vector2[] hull, Vector2[][] holes) {
            this.hull = hull;
            this.holes = holes;
        }
        
        public Shape(Vector2[][] pathList) {
	        this.hull = pathList[0];
	        int n = pathList.Length - 1;
	        var list = new List<Vector2[]>();
	        if (n > 0) {
		        for (int j = 0; j < n; j++) {
			        list.Add(pathList[j + 1]);
		        }
	        }
	        this.holes = list.ToArray();
        }

    }

}