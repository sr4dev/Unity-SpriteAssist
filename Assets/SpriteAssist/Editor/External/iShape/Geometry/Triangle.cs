using UnityEngine;

namespace iShape.Geometry {

    public struct Triangle {

        public readonly Vector2 a;
        public readonly Vector2 b;
        public readonly Vector2 c;
    
        public Triangle(Vector2 a, Vector2 b, Vector2 c) {
            this.a = a;
            this.b = b;
            this.c = c;
        }
 
        public float Area => 0.5f * (a.x * (c.y - b.y) + b.x * (a.y - c.y) + c.x * (b.y - a.y));

        public Circle Circumscribed => Triangle.FindCircumscribed(a, b, c);

        public Circle Inscribed => Triangle.FindInscribed(a, b, c);
    
        public static Circle FindCircumscribed(Vector2 a, Vector2 b, Vector2 c) {
            float d = 2 * (a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y));
            float x = ((a.x * a.x + a.y * a.y) * (b.y - c.y) + (b.x * b.x + b.y * b.y) * (c.y - a.y) +
                       (c.x * c.x + c.y * c.y) * (a.y - b.y)) / d;
            float y = ((a.x * a.x + a.y * a.y) * (c.x - b.x) + (b.x * b.x + b.y * b.y) * (a.x - c.x) +
                       (c.x * c.x + c.y * c.y) * (b.x - a.x)) / d;

            float r = Mathf.Sqrt((a.x - x) * (a.x - x) + (a.y - y) * (a.y - y));

            return new Circle(new Vector2(x, y), r);
        }
    
        public static Circle FindInscribed(Vector2 a, Vector2 b, Vector2 c) {
            float ABx = a.x - b.x;
            float ABy = a.y - b.y;
            float AB = Mathf.Sqrt(ABx * ABx + ABy * ABy);

            float ACx = a.x - c.x;
            float ACy = a.y - c.y;
            float AC = Mathf.Sqrt(ACx * ACx + ACy * ACy);

            float BCx = b.x - c.x;
            float BCy = b.y - c.y;
            float BC = Mathf.Sqrt(BCx * BCx + BCy * BCy);

            float p = AB + BC + AC;

            float Ox = (BC * a.x + AC * b.x + AB * c.x) / p;
            float Oy = (BC * a.y + AC * b.y + AB * c.y) / p;

            float r = Mathf.Sqrt((-BC + AC + AB) * (BC - AC + AB) * (BC + AC - AB) / (4 * p));
            
            return new Circle(new Vector2(Ox, Oy), r);
        }
        
    }

}