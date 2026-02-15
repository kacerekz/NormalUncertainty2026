using MyLibrary;
using System;
using System.Numerics;

namespace NormalUncertainty.Experiments.Convergence._3D
{
    public class Scenario3D
    {
        public Vector3 BoundsAMin, BoundsAMax;
        public Vector3 BoundsBMin, BoundsBMax;
        public Vector3 BoundsCMin, BoundsCMax;

        // Configuration
        private const float MinSize = 0.000001f, MaxSize = 1.0f;
        private const float MinDist = 0.000001f, MaxDist = 10.0f;

        public Scenario3D(Random r)
        {
            // Center A is origin
            Vector3 centerA = Vector3.Zero;

            // Center B and C are random points on a sphere shell around A
            Vector3 centerB = GenerateRandomOffset(r);
            Vector3 centerC = GenerateRandomOffset(r);

            // Generate the actual boxes
            (BoundsAMin, BoundsAMax) = GenerateBounds(r, centerA);
            (BoundsBMin, BoundsBMax) = GenerateBounds(r, centerB);
            (BoundsCMin, BoundsCMax) = GenerateBounds(r, centerC);
        }

        private Vector3 GenerateRandomOffset(Random r)
        {
            Vector3 v;
            float sqMag;

            // 1. Sample a point within the unit sphere (rejection sampling)
            do
            {
                v = new Vector3(
                    (float)r.NextDouble() * 2 - 1,
                    (float)r.NextDouble() * 2 - 1,
                    (float)r.NextDouble() * 2 - 1
                );
                sqMag = v.LengthSquared();

                // Keep it if it's inside the unit sphere and not at the zero origin
            } while (sqMag > 1.0f || sqMag < 0.0001f);

            // 2. Normalize to get a random direction
            Vector3 direction = v / MathF.Sqrt(sqMag);

            // 3. Apply the desired range
            float distance = MinDist + (float)r.NextDouble() * (MaxDist - MinDist);

            return direction * distance;
        }

        private (Vector3, Vector3) GenerateBounds(Random r, Vector3 center)
        {
            float sx = (float)r.NextDouble() * (MaxSize - MinSize) + MinSize;
            float sy = (float)r.NextDouble() * (MaxSize - MinSize) + MinSize;
            float sz = (float)r.NextDouble() * (MaxSize - MinSize) + MinSize;
            Vector3 halfSize = new Vector3(sx, sy, sz) * 0.5f;
            return (center - halfSize, center + halfSize);
        }
    }
}