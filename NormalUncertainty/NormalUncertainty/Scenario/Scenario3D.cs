using MyLibrary;
using System;
using System.Numerics;

namespace NormalUncertainty.Scenario
{
    public class Scenario3D
    {
        public Vector3 BoundsAMin { get; }
        public Vector3 BoundsAMax { get; }
        public Vector3 BoundsBMin { get; }
        public Vector3 BoundsBMax { get; }
        public Vector3 BoundsCMin { get; }
        public Vector3 BoundsCMax { get; }

        public Scenario3D(Vector3 aMin, Vector3 aMax, Vector3 bMin, Vector3 bMax, Vector3 cMin, Vector3 cMax)
        {
            BoundsAMin = aMin; BoundsAMax = aMax;
            BoundsBMin = bMin; BoundsBMax = bMax;
            BoundsCMin = cMin; BoundsCMax = cMax;
        }

        public Scenario3D NormalizedX()
        {
            Vector3 shift = -BoundsAMin;
            float scale = 1.0f / (BoundsAMax.X - BoundsAMin.X);

            Vector3 Norm(Vector3 v) => (v + shift) * scale;

            return new Scenario3D(
                Norm(BoundsAMin), Norm(BoundsAMax),
                Norm(BoundsBMin), Norm(BoundsBMax),
                Norm(BoundsCMin), Norm(BoundsCMax)
            );
        }

        public float[] GetNetworkInputX()
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

        public Scenario3D Normalized()
        {
            Vector3 shift = -BoundsAMin;

            // Find the largest dimension of Cell A
            float dx = BoundsAMax.X - BoundsAMin.X;
            float dy = BoundsAMax.Y - BoundsAMin.Y;
            float dz = BoundsAMax.Z - BoundsAMin.Z;
            float maxSpan = MathF.Max(dx, MathF.Max(dy, dz));

            float scale = 1.0f / maxSpan;

            Vector3 Norm(Vector3 v) => (v + shift) * scale;
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
                BoundsAMax.X - BoundsAMin.X, // width
                BoundsAMax.Y - BoundsAMin.Y, // height
                BoundsAMax.Z - BoundsAMin.Z, // depth
                BoundsBMin.X, BoundsBMin.Y, BoundsBMin.Z,
                BoundsBMax.X, BoundsBMax.Y, BoundsBMax.Z,
                BoundsCMin.X, BoundsCMin.Y, BoundsCMin.Z,
                BoundsCMax.X, BoundsCMax.Y, BoundsCMax.Z,
            ];
        }
    }
}