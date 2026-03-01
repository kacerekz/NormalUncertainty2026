using SpanCompression.Structures;
using System.Numerics;

namespace SpanCompression
{
    public class MathUtils
    {
        public static Vector3 GetTriangleNormal(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            var n = Vector3.Cross(v2 - v1, v3 - v1);
            var nn = Vector3.Normalize(n);
            return nn;
        }

        public static double GetAngleBetween(Vector3 v1, Vector3 v2)
        {
            var dot = Vector3.Dot(v1, v2);
            var mag = v1.Length() * v2.Length();

            var d = dot / mag;
            d = Math.Clamp(d, -1, 1);

            var rad = Math.Acos(d);
            return ToDegrees(rad);
        }

        public static double ToDegrees(double radians)
        {
            return radians * 180 / Math.PI;
        }
    }
}