using MyLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace NormalUncertainty.Experiments.Convergence._2D
{
    public class Experiment06
    {
        private int scenarioCount = 10_000;
        private int maxSamples = 100_000;

        private Random r = new Random();

        public void Run()
        {
            // 1. Print Parameters
            Console.WriteLine("--- Experiment 06: Cache Integrity & Disparity Check ---");
            Console.WriteLine($"[Parameters]");
            Console.WriteLine($"Scenario Count:   {scenarioCount:N0}");
            Console.WriteLine($"Max Samples:      {maxSamples:N0}");
            Console.WriteLine($"Cache Capacity:   200,000 samples"); // Defined in HaltonCache
            Console.WriteLine();

            float maxDiff = 0;
            double sumDiff = 0;
            double sumSqDiff = 0;
            List<float> angleHistory = [];

            for (int i = 0; i < scenarioCount; i++)
            {
                Scenario2D scenario = new(r);

                // 2. Initialize Samplers
                // We compare the Ground Truth (Random) vs. the Optimized (Cached Halton)
                ISamplingStrategy2D truthSampler = new BasicSampler2D(scenario, r);
                ISamplingStrategy2D cachedSampler = new CachedHaltonSampler2D(scenario);

                // 3. Run for a set maximum number of samples (No Early Stopping)
                truthSampler.Sample(maxSamples);
                cachedSampler.Sample(maxSamples);

                // 4. Calculate Final Disparity
                Vector2 truthNormal = truthSampler.GetAverageNormal();
                Vector2 cachedNormal = cachedSampler.GetAverageNormal();

                float angleRad = MathUtil.UnsignedUnitVectorAngularDifferenceFast(truthNormal, cachedNormal);
                float angleDeg = MathUtil.ToDegrees(angleRad);

                // 5. Statistics
                angleHistory.Add(angleDeg);
                maxDiff = Math.Max(maxDiff, angleDeg);
                sumDiff += angleDeg;
                sumSqDiff += (double)angleDeg * angleDeg;

                if ((i + 1) % 1000 == 0) Console.Write(".");
            }
            Console.WriteLine("\n");

            // --- Final Statistical Output ---
            double avgDiff = sumDiff / scenarioCount;
            double stdDevDiff = Math.Sqrt((sumSqDiff - (sumDiff * sumDiff) / scenarioCount) / (scenarioCount - 1));

            angleHistory.Sort();
            float median = angleHistory[angleHistory.Count / 2];
            float p95 = angleHistory[(int)(angleHistory.Count * 0.95)];
            float p99 = angleHistory[(int)(angleHistory.Count * 0.99)];

            Console.WriteLine($"--- Final Disparity Results (Random vs Cached Halton) ---");
            Console.WriteLine($"Avg Difference: {avgDiff:F6}°");
            Console.WriteLine($"StdDev:         {stdDevDiff:F6}°");
            Console.WriteLine($"Median:         {median:F6}°");
            Console.WriteLine($"95th %:         {p95:F6}°");
            Console.WriteLine($"99th %:         {p99:F6}°");
            Console.WriteLine($"Max Difference: {maxDiff:F6}°");
            Console.WriteLine();

            Console.WriteLine("--- Integrity Check ---");
            // If the average difference is ~0.06, the cache is working perfectly.
            if (avgDiff < 0.1)
                Console.WriteLine("SUCCESS: Cached Halton matches previous Raw Halton accuracy levels.");
            else
                Console.WriteLine("WARNING: Disparity is higher than expected. Check cache indexing.");
        }
    }
}