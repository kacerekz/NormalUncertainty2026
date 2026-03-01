using System.Collections.Generic;
using System.Numerics;

namespace NormalUncertainty.Samplers._2D
{
    public abstract class Sampler2D
    {
        public List<Vector2> NormalHistory { get; } = [];

        public abstract int Sample(int count);

        public Vector2 GetAverageNormal()
        {
            if (NormalHistory.Count == 0) return Vector2.Zero;

            Vector2 sum = Vector2.Zero;
            foreach (var n in NormalHistory) sum += n;
            return Vector2.Normalize(sum);
        }
    }
}