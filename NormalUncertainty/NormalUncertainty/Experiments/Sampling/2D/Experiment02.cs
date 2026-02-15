using MyLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace NormalUncertainty.Experiments.Convergence._2D
{
    public class Experiment02
    {
        private int scenarioCount = 10_000;
        private int maxSamples = 100_000;

        private Random r = new Random();

        public void Run()
        {
            Console.WriteLine("--- Experiment 02: Halton vs Random Consistency ---");
            Console.WriteLine($"scenarioCount: {scenarioCount:N0}");
            Console.WriteLine($"maxSamples:    {maxSamples:N0}");
            Console.WriteLine();

            float maxDifference = 0;
            double sumDifference = 0;
            double sumSqDifference = 0;

            List<float> angleHistory = [];

            // Main Experiment Loop
            for (int i = 0; i < scenarioCount; i++)
            {
                // 1. Create a shared scenario
                Scenario2D scenario = new(r);

                // 2. Initialize both strategies
                ISamplingStrategy2D haltonSampler = new HaltonSampler2D(scenario);
                ISamplingStrategy2D randomSampler = new BasicSampler2D(scenario, r);

                // 3. Run both for the full sample count
                haltonSampler.Sample(maxSamples);
                randomSampler.Sample(maxSamples);

                // 4. Compare the resulting Average Normals
                Vector2 haltonNormal = haltonSampler.GetAverageNormal();
                Vector2 randomNormal = randomSampler.GetAverageNormal();

                float angleRad = MathUtil.UnsignedUnitVectorAngularDifferenceFast(haltonNormal, randomNormal);
                float angleDeg = MathUtil.ToDegrees(angleRad);

                // 5. Collect Stats
                angleHistory.Add(angleDeg);
                maxDifference = Math.Max(maxDifference, angleDeg);
                sumDifference += angleDeg;
                sumSqDifference += (angleDeg * angleDeg);

                // Optional: Progress indicator every 1000 scenarios
                if ((i + 1) % 1000 == 0) Console.Write(".");
            }
            Console.WriteLine("\n");

            // Calculate Statistics
            double avgDifference = sumDifference / scenarioCount;
            double stdDevDifference = Math.Sqrt((sumSqDifference - (sumDifference * sumDifference) / scenarioCount) / (scenarioCount - 1));

            angleHistory.Sort();
            float median = angleHistory[angleHistory.Count / 2];
            float p95 = angleHistory[(int)(angleHistory.Count * 0.95)];
            float p99 = angleHistory[(int)(angleHistory.Count * 0.99)];

            Console.WriteLine($"--- Angular Difference (Halton vs Random) [Degrees] ---");
            Console.WriteLine($"Max:    {maxDifference:F6}");
            Console.WriteLine($"Avg:    {avgDifference:F6}");
            Console.WriteLine($"StdDev: {stdDevDifference:F6}");
            Console.WriteLine($"Median: {median:F6}");
            Console.WriteLine($"95th %: {p95:F6}");
            Console.WriteLine($"99th %: {p99:F6}");
        }
    }
}