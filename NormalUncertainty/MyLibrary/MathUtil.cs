using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace MyLibrary
{
    public class MathUtil
    {
        public static float ToRadians(float angle)
        {
            return angle * (MathF.PI / 180f);
        }

        public static float ToDegrees(float angle)
        {
            return angle * (180f / MathF.PI);
        }

        public static float UnsignedAngularDifference(Vector2 u, Vector2 v)
        {
            var lenU = u.Length();
            var lenV = v.Length();
            var dot = Vector2.Dot(u, v);
            return MathF.Acos(dot / (lenU * lenV));
        }

        public static float UnsignedAngularDifference(Vector3 u, Vector3 v)
        {
            float length = u.Length() * v.Length();

            // Prevent division by zero
            if (length < 1e-10f) return 0;

            var dot = Vector3.Dot(u, v);
            dot /= length;

            // Clamp to range [-1,1]
            // Prevents NaN values if dot is outside range due to float precision
            float clamped = Math.Clamp(dot, -1f, 1f);

            return MathF.Acos(clamped);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float UnsignedUnitVectorAngularDifferenceFast(Vector3 u, Vector3 v)
        {
            float dot = Vector3.Dot(u, v);
            return MathF.Acos(Math.Clamp(dot, -1f, 1f));
        }
    }
}
