using MyLibrary;
using NormalUncertainty.Scenario;
using System.Collections.Generic;
using System.Numerics;

namespace NormalUncertainty.Samplers._3D
{
    public class CachedHaltonSampler3D : Sampler3D
    {
        private readonly Scenario3D _s;
        private int _idx = 1;

        public CachedHaltonSampler3D(Scenario3D s)
        { 
            _s = s;
        }

        public override int Sample(int count)
        {
            int added = 0;
            for (int i = 0; i < count; i++)
            {
                Vector3 pA = GetPoint(_s.BoundsAMin, _s.BoundsAMax, 0);
                Vector3 pB = GetPoint(_s.BoundsBMin, _s.BoundsBMax, 3);
                Vector3 pC = GetPoint(_s.BoundsCMin, _s.BoundsCMax, 6);

                _idx++;

                Vector3 u = pB - pA;
                Vector3 v = pC - pA;

                if (u != Vector3.Zero && v != Vector3.Zero)
                {
                    Vector3 uNorm = Vector3.Normalize(u);
                    Vector3 vNorm = Vector3.Normalize(v);
                    Vector3 normal = Vector3.Cross(uNorm, vNorm);

                    if (normal.LengthSquared() > 1e-6f)
                    {
                        NormalHistory.Add(Vector3.Normalize(normal));
                        added++;
                    }
                }
            }
            return added;
        }

        private Vector3 GetPoint(Vector3 min, Vector3 max, int dimOffset)
        {
            float tx = HaltonCache.Get(_idx, dimOffset);
            float ty = HaltonCache.Get(_idx, dimOffset + 1);
            float tz = HaltonCache.Get(_idx, dimOffset + 2);

            return new Vector3(
                min.X + tx * (max.X - min.X),
                min.Y + ty * (max.Y - min.Y),
                min.Z + tz * (max.Z - min.Z)
            );
        }
    }
}