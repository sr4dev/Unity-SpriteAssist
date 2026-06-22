using UnityEngine;

namespace iShape.Geometry {

    public struct Circle {
        
        public readonly Vector2 Center;
        public readonly float Radius;
    
        public Circle(Vector2 center, float radius) {
            this.Center = center;
            this.Radius = radius;
        }
    }

}