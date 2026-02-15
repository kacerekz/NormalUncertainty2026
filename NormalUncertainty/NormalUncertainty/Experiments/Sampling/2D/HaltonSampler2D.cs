using MyLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NormalUncertainty.Experiments.Convergence._2D
{
    public class HaltonSampler2D : ISamplingStrategy2D
    {
        private readonly Scenario2D _scenario;
        private int _sampleIndex = 1; // Halton sequence usually starts at index 1 to avoid 0.0

        public List<Vector2> NormalHistory { get; } = new();

        public HaltonSampler2D(Scenario2D scenario)
        {
            _scenario = scenario;
        }

        public int Sample(int count)
        {
            int samplesAdded = 0;
            // We loop until we have added 'count' valid samples. 
            // Note: In rare cases where a normal is length 0, we might skip an index, 
            // so we loop based on success count, but increment the Halton index regardless.
            while (samplesAdded < count)
            {
                // 1. Generate 4D Halton point
                // Base 2 for Ax, Base 3 for Ay
                float tAx = Halton.Get(_sampleIndex, 2);
                float tAy = Halton.Get(_sampleIndex, 3);

                // Base 5 for Bx, Base 7 for By
                float tBx = Halton.Get(_sampleIndex, 5);
                float tBy = Halton.Get(_sampleIndex, 7);

                _sampleIndex++;

                // 2. Map to Scenario Bounds
                Vector2 pA = new Vector2(
                    _scenario.boundsAMin.X + tAx * (_scenario.boundsAMax.X - _scenario.boundsAMin.X),
                    _scenario.boundsAMin.Y + tAy * (_scenario.boundsAMax.Y - _scenario.boundsAMin.Y)
                );

                Vector2 pB = new Vector2(
                    _scenario.boundsBMin.X + tBx * (_scenario.boundsBMax.X - _scenario.boundsBMin.X),
                    _scenario.boundsBMin.Y + tBy * (_scenario.boundsBMax.Y - _scenario.boundsBMin.Y)
                );

                // 3. Compute Normal
                Vector2 line = pB - pA;
                Vector2 normal = new Vector2(-line.Y, line.X);

                if (normal.LengthSquared() > 0.00001f)
                {
                    NormalHistory.Add(Vector2.Normalize(normal));
                    samplesAdded++;
                }
            }
            return samplesAdded;
        }

        public Vector2 GetAverageNormal()
        {
            Vector2 average = Vector2.Zero;
            foreach (var n in NormalHistory)
            {
                average += n;
            }
            return Vector2.Normalize(average);
        }
    }
}
