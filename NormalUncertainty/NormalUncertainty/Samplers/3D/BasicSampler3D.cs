using MyLibrary;
using NormalUncertainty.Scenario;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace NormalUncertainty.Samplers._3D
{
    public class BasicSampler3D : Sampler3D
    {
        private readonly Scenario3D _s;
        private readonly Random _r;

        public BasicSampler3D(Scenario3D s, Random r) 
        { 
            _s = s; 
            _r = r; 
        }

        public override int Sample(int count)
        {
            int added = 0;

            while (added < count)
            {
                Vector3 pA = RandomPoint(_s.BoundsAMin, _s.BoundsAMax);
                Vector3 pB = RandomPoint(_s.BoundsBMin, _s.BoundsBMax);
                Vector3 pC = RandomPoint(_s.BoundsCMin, _s.BoundsCMax);

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

        private Vector3 RandomPoint(Vector3 min, Vector3 max)
        {
            return new Vector3(
                min.X + (float)_r.NextDouble() * (max.X - min.X),
                min.Y + (float)_r.NextDouble() * (max.Y - min.Y),
                min.Z + (float)_r.NextDouble() * (max.Z - min.Z)
            );
        }
    }
}