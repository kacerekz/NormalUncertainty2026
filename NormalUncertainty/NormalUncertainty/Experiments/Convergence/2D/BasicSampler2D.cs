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
    public class BasicSampler2D(Scenario2D scenario)
    {
        public Scenario2D scenario = scenario;

        public List<Vector2> NormalHistory { get; set; } = [];

        public void SampleUntil(Random r, int samplesPerRun, float maxChange)
        {
            Vector2 lastAverage;
            Vector2 currentAverage;
            float angle;

            Sample(r, samplesPerRun);
            currentAverage = GetAverageNormal();

            do
            {
                lastAverage = currentAverage;
                Sample(r, samplesPerRun);
                currentAverage = GetAverageNormal();
                angle = MathUtil.UnsignedUnitVectorAngularDifferenceFast(lastAverage, currentAverage);
                angle = MathUtil.ToDegrees(angle);
            }
            while (angle > maxChange);
        }

        public void Sample(Random r, int n)
        {
            int samplesTaken = 0;

            while (samplesTaken < n)
            {
                // Sample A
                float tAx = (float)r.NextDouble();
                float tAy = (float)r.NextDouble();
                Vector2 pA = new(
                    scenario.boundsAMin.X + tAx * (scenario.boundsAMax.X - scenario.boundsAMin.X),
                    scenario.boundsAMin.Y + tAy * (scenario.boundsAMax.Y - scenario.boundsAMin.Y)
                );

                // Sample B
                float tBx = (float)r.NextDouble();
                float tBy = (float)r.NextDouble();
                Vector2 pB = new(
                    scenario.boundsBMin.X + tBx * (scenario.boundsBMax.X - scenario.boundsBMin.X),
                    scenario.boundsBMin.Y + tBy * (scenario.boundsBMax.Y - scenario.boundsBMin.Y)
                );

                // Compute Normal
                Vector2 line = pB - pA;
                Vector2 normal = new(-line.Y, line.X);

                if (normal.LengthSquared() > 0.00001f)
                {
                    var normalized = Vector2.Normalize(normal);
                    NormalHistory.Add(normalized);
                    samplesTaken++;
                }
            }
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
