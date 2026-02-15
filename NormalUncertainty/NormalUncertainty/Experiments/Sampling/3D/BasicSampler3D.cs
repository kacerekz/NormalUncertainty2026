using MyLibrary;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace NormalUncertainty.Experiments.Convergence._3D
{
    public class BasicSampler3D : ISamplingStrategy3D
    {
        private readonly Scenario3D _s;
        private readonly Random _r;
        public List<Vector3> NormalHistory { get; } = new();

        public BasicSampler3D(Scenario3D s, Random r) { _s = s; _r = r; }

        public int Sample(int count)
        {
            int added = 0;
            for (int i = 0; i < count; i++)
            {
                Vector3 pA = RandomPoint(_s.BoundsAMin, _s.BoundsAMax);
                Vector3 pB = RandomPoint(_s.BoundsBMin, _s.BoundsBMax);
                Vector3 pC = RandomPoint(_s.BoundsCMin, _s.BoundsCMax);

                // Normal = (B-A) x (C-A)
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

        private Vector3 RandomPoint(Vector3 min, Vector3 max)
        {
            return new Vector3(
                min.X + (float)_r.NextDouble() * (max.X - min.X),
                min.Y + (float)_r.NextDouble() * (max.Y - min.Y),
                min.Z + (float)_r.NextDouble() * (max.Z - min.Z)
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