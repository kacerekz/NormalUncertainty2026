using MyLibrary;
using System.Collections.Generic;
using System.Numerics;

namespace NormalUncertainty.Experiments.Convergence._3D
{
    public class CornerSampler3D : ISamplingStrategy3D
    {
        private readonly Scenario3D _s;
        private int _index = 0;
        private const int TotalSamples = 512; // 8 * 8 * 8

        public List<Vector3> NormalHistory { get; } = new();

        public CornerSampler3D(Scenario3D s)
        {
            _s = s;
        }

        public int Sample(int count)
        {
            int added = 0;

            // We iterate through the Cartesian product of 3 boxes (A, B, C)
            // Each box has 8 corners. 
            // We flatten the loop: index 0 to 511.

            while (_index < TotalSamples && added < count)
            {
                // Decode index into 3 indices (0-7) for the three boxes
                // boxC changes fastest, then boxB, then boxA
                int idxA = (_index / 64) % 8;
                int idxB = (_index / 8) % 8;
                int idxC = _index % 8;

                Vector3 pA = GetCorner(_s.BoundsAMin, _s.BoundsAMax, idxA);
                Vector3 pB = GetCorner(_s.BoundsBMin, _s.BoundsBMax, idxB);
                Vector3 pC = GetCorner(_s.BoundsCMin, _s.BoundsCMax, idxC);

                _index++;

                Vector3 u = pB - pA;
                Vector3 v = pC - pA;
                Vector3 normal = Vector3.Cross(u, v);

                // Even corners can produce degenerate triangles (collinear points)
                if (normal.LengthSquared() > 1e-6f)
                {   
                    NormalHistory.Add(Vector3.Normalize(normal));
                    added++;
                }
            }
            return added;
        }

        // Helper: Gets one of the 8 corners based on a 3-bit index (0-7)
        private Vector3 GetCorner(Vector3 min, Vector3 max, int i)
        {
            return new Vector3(
                (i & 1) == 0 ? min.X : max.X, // Bit 0 -> X
                (i & 2) == 0 ? min.Y : max.Y, // Bit 1 -> Y
                (i & 4) == 0 ? min.Z : max.Z  // Bit 2 -> Z
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