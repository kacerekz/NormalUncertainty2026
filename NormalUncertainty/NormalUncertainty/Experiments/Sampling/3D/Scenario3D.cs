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
            Vector3 centerA = Vector3.Zero;
            Vector3 centerB = GenerateRandomOffset(r);
            Vector3 centerC = GenerateRandomOffset(r);

            (BoundsAMin, BoundsAMax) = GenerateBounds(r, centerA);
            (BoundsBMin, BoundsBMax) = GenerateBounds(r, centerB);
            (BoundsCMin, BoundsCMax) = GenerateBounds(r, centerC);
        }

        public Scenario3D(Vector3 aMin, Vector3 aMax, Vector3 bMin, Vector3 bMax, Vector3 cMin, Vector3 cMax)
        {
            BoundsAMin = aMin; BoundsAMax = aMax;
            BoundsBMin = bMin; BoundsBMax = bMax;
            BoundsCMin = cMin; BoundsCMax = cMax;
        }

        public Scenario3D Normalized()
        {
            Vector3 shift = -BoundsAMin;
            float scale = 1.0f / (BoundsAMax.X - BoundsAMin.X);

            Vector3 Norm(Vector3 v) => (v + shift) * scale;

            // 2. Return new object
            return new Scenario3D(
                Norm(BoundsAMin), Norm(BoundsAMax),
                Norm(BoundsBMin), Norm(BoundsBMax),
                Norm(BoundsCMin), Norm(BoundsCMax)
            );
        }

        public float[] GetNetworkInput()
        {
            return
            [
                BoundsAMax.Y - BoundsAMin.Y, // height
                BoundsAMax.Z - BoundsAMin.Z, // depth
                BoundsBMin.X, BoundsBMin.Y, BoundsBMin.Z,
                BoundsBMax.X, BoundsBMax.Y, BoundsBMax.Z,
                BoundsCMin.X, BoundsCMin.Y, BoundsCMin.Z,
                BoundsCMax.X, BoundsCMax.Y, BoundsCMax.Z,
            ];
        }

        private Vector3 GenerateRandomOffset(Random r)
        {
            Vector3 v;
            float sqMag;
            do
            {
                v = new Vector3(
                    (float)r.NextDouble() * 2 - 1,
                    (float)r.NextDouble() * 2 - 1,
                    (float)r.NextDouble() * 2 - 1
                );
                sqMag = v.LengthSquared();
            } while (sqMag > 1.0f || sqMag < 0.0001f);

            Vector3 direction = v / MathF.Sqrt(sqMag);
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