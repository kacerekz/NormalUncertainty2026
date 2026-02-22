using System.Collections.Generic;
using System.Numerics;

namespace NormalUncertainty.Experiments.Convergence._3D
{
    public abstract class Sampler3D
    {
        public List<Vector3> NormalHistory { get; } = [];

        public abstract int Sample(int count);

        public Vector3 GetAverageNormal()
        {
            if (NormalHistory.Count == 0) return Vector3.Zero;

            Vector3 sum = Vector3.Zero;
            foreach (var n in NormalHistory) sum += n;
            return Vector3.Normalize(sum);
        }
    }
}