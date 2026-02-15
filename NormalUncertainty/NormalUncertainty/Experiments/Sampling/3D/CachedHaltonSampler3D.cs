using MyLibrary;
using System.Collections.Generic;
using System.Numerics;

namespace NormalUncertainty.Experiments.Convergence._3D
{
    public class CachedHaltonSampler3D : ISamplingStrategy3D
    {
        private readonly Scenario3D _s;
        private int _idx = 0;
        public List<Vector3> NormalHistory { get; } = new();

        public CachedHaltonSampler3D(Scenario3D s) { _s = s; HaltonCache3D.Initialize(); }

        public int Sample(int count)
        {
            int added = 0;
            for (int i = 0; i < count; i++)
            {
                // Point A (Dims 0,1,2)
                Vector3 pA = GetPoint(_s.BoundsAMin, _s.BoundsAMax, 0);
                // Point B (Dims 3,4,5)
                Vector3 pB = GetPoint(_s.BoundsBMin, _s.BoundsBMax, 3);
                // Point C (Dims 6,7,8)
                Vector3 pC = GetPoint(_s.BoundsCMin, _s.BoundsCMax, 6);

                _idx++;

                Vector3 u = pB - pA;
                Vector3 v = pC - pA;
                Vector3 normal = Vector3.Cross(u, v);

                if (normal.LengthSquared() > 1e-6f)
                {
                    NormalHistory.Add(Vector3.Normalize(normal));
                    added++;
                }
            }
            return added;
        }

        private Vector3 GetPoint(Vector3 min, Vector3 max, int dimOffset)
        {
            float tx = HaltonCache3D.Get(_idx, dimOffset);
            float ty = HaltonCache3D.Get(_idx, dimOffset + 1);
            float tz = HaltonCache3D.Get(_idx, dimOffset + 2);

            return new Vector3(
                min.X + tx * (max.X - min.X),
                min.Y + ty * (max.Y - min.Y),
                min.Z + tz * (max.Z - min.Z)
            );
        }

        public Vector3 GetAverageNormal()
        {
            Vector3 sum = Vector3.Zero;
            foreach (var n in NormalHistory) sum += n;
            return Vector3.Normalize(sum);
        }
    }
}