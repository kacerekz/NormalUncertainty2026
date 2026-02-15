using MyLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace NormalUncertainty.Experiments.Convergence._2D
{
    public class Experiment03
    {
        private int scenarioCount = 10_000;
        private int samplesPerRun = 100; // Smaller batch size to catch early convergence differences
        private float maxChange = 0.01f; // degrees

        private Random r = new Random();

        public void Run()
        {
            Console.WriteLine("--- Experiment 03: Convergence Speed (Halton vs Random) ---");
            Console.WriteLine($"scenarioCount: {scenarioCount:N0}");
            Console.WriteLine($"samplesPerRun: {samplesPerRun:N0}");
            Console.WriteLine($"maxChange:     {maxChange:F6} degrees");
            Console.WriteLine();

            // Store sample counts for both methods
            List<int> randomSamples = [];
            List<int> haltonSamples = [];

            // Store the final angular difference between their results 
            // (to ensure they didn't just stop early at wrong values)
            List<float> finalDifferences = [];

            for (int i = 0; i < scenarioCount; i++)
            {
                Scenario2D scenario = new(r);

                // 1. Initialize Strategies
                ISamplingStrategy2D randomSampler = new BasicSampler2D(scenario, r);
                ISamplingStrategy2D haltonSampler = new HaltonSampler2D(scenario);

                // 2. Run both until they satisfy the condition
                SampleUntil(randomSampler, samplesPerRun, maxChange);
                SampleUntil(haltonSampler, samplesPerRun, maxChange);

                // 3. Record Results
                randomSamples.Add(randomSampler.NormalHistory.Count);
                haltonSamples.Add(haltonSampler.NormalHistory.Count);

                // 4. Sanity Check: How far apart are their final answers?
                Vector2 n1 = randomSampler.GetAverageNormal();
                Vector2 n2 = haltonSampler.GetAverageNormal();
                float diff = MathUtil.ToDegrees(MathUtil.UnsignedUnitVectorAngularDifferenceFast(n1, n2));
                finalDifferences.Add(diff);

                if ((i + 1) % 1000 == 0) Console.Write(".");
            }
            Console.WriteLine("\n");

            // --- Analyze Speed (Samples Needed) ---
            PrintStats("Random Sampler (Samples)", randomSamples);
            PrintStats("Halton Sampler (Samples)", haltonSamples);

            // --- Analyze Accuracy (Final Agreement) ---
            // Ideally this is small. If Halton stops way earlier but has a huge difference 
            // from Random, it might be "stabilizing" prematurely (getting stuck).
            PrintFloatStats("Final Angular Agreement", finalDifferences);
        }

        private void PrintStats(string name, List<int> data)
        {
            data.Sort();
            double avg = data.Average();
            double sumSq = data.Sum(d => Math.Pow(d - avg, 2));
            double stdDev = Math.Sqrt(sumSq / (data.Count - 1));

            Console.WriteLine($"--- {name} ---");
            Console.WriteLine($"Avg:    {avg:F2}");
            Console.WriteLine($"StdDev: {stdDev:F2}");
            Console.WriteLine($"Median: {data[data.Count / 2]}");
            Console.WriteLine($"Min:    {data[0]}");
            Console.WriteLine($"Max:    {data[^1]}");
            Console.WriteLine();
        }

        private void PrintFloatStats(string name, List<float> data)
        {
            data.Sort();
            double avg = data.Average();

            Console.WriteLine($"--- {name} [Degrees] ---");
            Console.WriteLine($"Avg Diff: {avg:F4}");
            Console.WriteLine($"Median:   {data[data.Count / 2]:F4}");
            Console.WriteLine($"95th %:   {data[(int)(data.Count * 0.95)]:F4}");
            Console.WriteLine($"99th %:   {data[(int)(data.Count * 0.99)]:F4}"); 
            Console.WriteLine($"Max Diff: {data[^1]:F4}");
            Console.WriteLine();
        }

        private void SampleUntil(ISamplingStrategy2D sampler, int batchSize, float maxChangeDegrees)
        {
            Vector2 lastAverage;
            Vector2 currentAverage;
            float angleDiff;

            // 1. Initial Batch
            sampler.Sample(batchSize);
            currentAverage = sampler.GetAverageNormal();

            // 2. Loop
            do
            {
                lastAverage = currentAverage;
                int added = sampler.Sample(batchSize);
                if (added == 0) break;

                currentAverage = sampler.GetAverageNormal();

                float angleRad = MathUtil.UnsignedUnitVectorAngularDifferenceFast(lastAverage, currentAverage);
                angleDiff = MathUtil.ToDegrees(angleRad);

                // Safety break for infinite loops (optional but good practice)
                if (sampler.NormalHistory.Count > 200_000) break;
            }
            while (angleDiff > maxChangeDegrees);
        }
    }
}