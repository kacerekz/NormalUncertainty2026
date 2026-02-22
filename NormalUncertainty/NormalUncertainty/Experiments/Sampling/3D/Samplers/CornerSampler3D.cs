using MyLibrary;
using System.Numerics;

namespace NormalUncertainty.Experiments.Convergence._3D
{
    public class CornerSampler3D : Sampler3D
    {
        private readonly Scenario3D _s;
        private bool _hasRun = false;

        public CornerSampler3D(Scenario3D s)
        {
            _s = s;
        }

        public override int Sample(int count)
        {
            if (_hasRun) return 0;

            int added = 0;

            for (int i = 0; i < 8; i++)
            {
                Vector3 pA = GetCorner(_s.BoundsAMin, _s.BoundsAMax, i);

                for (int j = 0; j < 8; j++)
                {
                    Vector3 pB = GetCorner(_s.BoundsBMin, _s.BoundsBMax, j);

                    for (int k = 0; k < 8; k++)
                    {
                        Vector3 pC = GetCorner(_s.BoundsCMin, _s.BoundsCMax, k);

                        Vector3 u = pB - pA;
                        Vector3 v = pC - pA;
                        Vector3 normal = Vector3.Cross(u, v);

                        if (normal.LengthSquared() > 1e-6f)
                        {
                            NormalHistory.Add(Vector3.Normalize(normal));
                            added++;
                        }
                    }
                }
            }

            _hasRun = true;
            return added;
        }

        // Helper: Gets one of the 8 corners based on a 3-bit index (0-7)
        // 000 = Min,Min,Min | 111 = Max,Max,Max
        private Vector3 GetCorner(Vector3 min, Vector3 max, int i)
        {
            return new Vector3(
                (i & 1) == 0 ? min.X : max.X, // Bit 0 -> X
                (i & 2) == 0 ? min.Y : max.Y, // Bit 1 -> Y
                (i & 4) == 0 ? min.Z : max.Z  // Bit 2 -> Z
            );
        }
    }
}