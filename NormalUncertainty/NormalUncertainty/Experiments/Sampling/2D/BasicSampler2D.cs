using MyLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NormalUncertainty.Experiments.Convergence._2D
{
    public class BasicSampler2D : ISamplingStrategy2D
    {
        private readonly Scenario2D _scenario;
        private readonly Random _random;

        public List<Vector2> NormalHistory { get; } = new();

        public BasicSampler2D(Scenario2D scenario, Random random)
        {
            _scenario = scenario;
            _random = random;
        }

        public int Sample(int count)
        {
            int samplesTaken = 0;
            while (samplesTaken < count)
            {
                // Sample A
                float tAx = (float)_random.NextDouble();
                float tAy = (float)_random.NextDouble();
                Vector2 pA = new(
                    _scenario.boundsAMin.X + tAx * (_scenario.boundsAMax.X - _scenario.boundsAMin.X),
                    _scenario.boundsAMin.Y + tAy * (_scenario.boundsAMax.Y - _scenario.boundsAMin.Y)
                );

                // Sample B
                float tBx = (float)_random.NextDouble();
                float tBy = (float)_random.NextDouble();
                Vector2 pB = new(
                    _scenario.boundsBMin.X + tBx * (_scenario.boundsBMax.X - _scenario.boundsBMin.X),
                    _scenario.boundsBMin.Y + tBy * (_scenario.boundsBMax.Y - _scenario.boundsBMin.Y)
                );

                // Compute Normal
                Vector2 line = pB - pA;
                Vector2 normal = new(-line.Y, line.X);

                if (normal.LengthSquared() > 0.00001f)
                {
                    NormalHistory.Add(Vector2.Normalize(normal));
                    samplesTaken++;
                }
            }
            return samplesTaken;
        }

        public Vector2 GetAverageNormal()
        {
            Vector2 average = Vector2.Zero;
            for (int i = 0; i < NormalHistory.Count; i++)
            {
                average += NormalHistory[i];
            }

            return Vector2.Normalize(average);
        }
    }
}
