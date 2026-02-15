using MyLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace NormalUncertainty.Experiments.Convergence._3D
{
    public class Experiment08
    {
        private int scenarioCount = 100_000;
        private Random r = new Random();

        // Halton Convergence Settings
        private int samplesPerBatch = 100;
        private float haltonThreshold = 0.01f;

        public void Run()
        {
            Console.WriteLine("--- Experiment 08: Corner Proxy vs. Halton Volume ---");
            Console.WriteLine($"Scenarios: {scenarioCount:N0}");
            Console.WriteLine($"Samples per batch: {samplesPerBatch:N0}");
            Console.WriteLine($"Halton Threshold: {haltonThreshold}°");
            Console.WriteLine($"Corner Samples: 512 (Fixed)");
            Console.WriteLine();

            List<double> disparities = [];
            List<double> haltonTimes = [];
            List<double> cornerTimes = [];

            // Warmup JIT
            Warmup();

            for (int i = 0; i < scenarioCount; i++)
            {
                Scenario3D s = new(r);

                // --- 1. Run Halton (Volume Ground Truth) ---
                var haltonSampler = new CachedHaltonSampler3D(s);

                long tH_Start = Stopwatch.GetTimestamp();
                SampleUntilConvergence(haltonSampler);
                long tH_End = Stopwatch.GetTimestamp();

                haltonTimes.Add((double)(tH_End - tH_Start) * 1000.0 / Stopwatch.Frequency);
                Vector3 truthNormal = haltonSampler.GetAverageNormal();

                // --- 2. Run Corners (Proxy) ---
                var cornerSampler = new CornerSampler3D(s);

                long tC_Start = Stopwatch.GetTimestamp();
                cornerSampler.Sample(512); // Compute all 512
                long tC_End = Stopwatch.GetTimestamp();

                cornerTimes.Add((double)(tC_End - tC_Start) * 1000.0 / Stopwatch.Frequency);
                Vector3 cornerNormal = cornerSampler.GetAverageNormal();

                // --- 3. Measure Disparity ---
                float diffRad = MathUtil.UnsignedUnitVectorAngularDifferenceFast(truthNormal, cornerNormal);
                disparities.Add(MathUtil.ToDegrees(diffRad));

                if ((i + 1) % 5000 == 0) Console.Write(".");
            }
            Console.WriteLine("\n");

            // --- Statistical Analysis ---

            // 1. Accuracy
            Console.WriteLine(">>> Accuracy (Angular Disparity) [Degrees]");
            PrintStats(disparities);

            // 2. Speed
            Console.WriteLine(">>> Speed: Halton Volume [ms]");
            PrintStats(haltonTimes);

            Console.WriteLine(">>> Speed: Corner Proxy [ms]");
            PrintStats(cornerTimes);

            // 3. Comparison
            double avgHalton = haltonTimes.Average();
            double avgCorner = cornerTimes.Average();
            Console.WriteLine(">>> Conclusion");
            Console.WriteLine($"Average Speedup: {(avgHalton / avgCorner):F2}x faster");

            double avgError = disparities.Average();
            Console.WriteLine($"Cost of Speedup: {avgError:F4}° average error");
        }

        private void Warmup()
        {
            Scenario3D s = new(r);
            new CornerSampler3D(s).Sample(512);
            SampleUntilConvergence(new CachedHaltonSampler3D(s));
        }

        private void PrintStats(List<double> data)
        {
            data.Sort();
            double avg = data.Average();
            double sumSq = data.Sum(d => Math.Pow(d - avg, 2));
            double stdDev = Math.Sqrt(sumSq / (data.Count - 1));

            Console.WriteLine($"Avg:    {avg:F4}");
            Console.WriteLine($"StdDev: {stdDev:F4}");
            Console.WriteLine($"Median: {data[data.Count / 2]:F4}");
            Console.WriteLine($"95th %: {data[(int)(data.Count * 0.95)]:F4}");
            Console.WriteLine($"99th %: {data[(int)(data.Count * 0.99)]:F4}");
            Console.WriteLine($"Max:    {data[^1]:F4}");
            Console.WriteLine();
        }

        private void SampleUntilConvergence(ISamplingStrategy3D sampler)
        {
            Vector3 cur = Vector3.Zero;
            float diff;

            // Initial batch
            sampler.Sample(samplesPerBatch);
            cur = sampler.GetAverageNormal();

            do
            {
                Vector3 prev = cur;
                int added = sampler.Sample(samplesPerBatch);
                if (added == 0) break;

                cur = sampler.GetAverageNormal();

                // Degenerate check
                if (prev == Vector3.Zero) { diff = 999; continue; }

                diff = MathUtil.ToDegrees(MathUtil.UnsignedUnitVectorAngularDifferenceFast(prev, cur));

                // Safety cap
                if (sampler.NormalHistory.Count > 50_000) break;

            } while (diff > haltonThreshold);
        }
    }
}