using MyLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace NormalUncertainty.Experiments.Convergence._3D
{
    public class Experiment07
    {
        private int scenarioCount = 100_000;
        private int samplesPerRun = 100;
        private float maxChange = 0.01f;

        private Random r = new Random();

        public void Run()
        {
            Console.WriteLine("--- Experiment 3D: Triangle Normal Convergence ---");
            Console.WriteLine($"Scenarios: {scenarioCount:N0}");
            Console.WriteLine($"Samples per run: {samplesPerRun:N0}");
            Console.WriteLine($"Threshold: {maxChange:F4}°");
            Console.WriteLine();

            List<int> randomSamples = [];
            List<int> haltonSamples = [];
            List<float> finalDiffs = [];

            long randomTotalTicks = 0;
            long haltonTotalTicks = 0;

            for (int i = 0; i < scenarioCount; i++)
            {
                Scenario3D s = new(r);

                // --- Random Sampler ---
                var randSampler = new BasicSampler3D(s, r);
                long t1 = Stopwatch.GetTimestamp();
                SampleUntil(randSampler);
                randomTotalTicks += (Stopwatch.GetTimestamp() - t1);
                randomSamples.Add(randSampler.NormalHistory.Count);

                // --- Halton Sampler ---
                var haltSampler = new CachedHaltonSampler3D(s);
                long t2 = Stopwatch.GetTimestamp();
                SampleUntil(haltSampler);
                haltonTotalTicks += (Stopwatch.GetTimestamp() - t2);
                haltonSamples.Add(haltSampler.NormalHistory.Count);

                // --- Compare Final Results ---
                float diff = MathUtil.ToDegrees(MathUtil.UnsignedUnitVectorAngularDifferenceFast(
                    randSampler.GetAverageNormal(),
                    haltSampler.GetAverageNormal()));
                finalDiffs.Add(diff);

                if ((i + 1) % 5000 == 0) Console.Write(".");
            }
            Console.WriteLine("\n");

            // --- Statistics Output ---
            PrintStats("Random Sampler (Samples)", randomSamples.Select(x => (double)x).ToList());
            PrintStats("Halton Sampler (Samples)", haltonSamples.Select(x => (double)x).ToList());
            PrintStats("Final Angular Disparity [Degrees]", finalDiffs.Select(x => (double)x).ToList());

            // --- Timing Output ---
            double randSec = (double)randomTotalTicks / Stopwatch.Frequency;
            double haltSec = (double)haltonTotalTicks / Stopwatch.Frequency;

            Console.WriteLine($"--- Timing ---");
            Console.WriteLine($"Random Total: {randSec:F2}s");
            Console.WriteLine($"Halton Total: {haltSec:F2}s");
            Console.WriteLine($"Net Speedup:  {(randSec / haltSec):F2}x");
        }

        private void PrintStats(string name, List<double> data)
        {
            if (data.Count == 0) return;

            data.Sort();
            double avg = data.Average();
            double sumSq = data.Sum(d => Math.Pow(d - avg, 2));
            double stdDev = Math.Sqrt(sumSq / (data.Count - 1));

            double median = data[data.Count / 2];
            double p95 = data[(int)(data.Count * 0.95)];
            double p99 = data[(int)(data.Count * 0.99)];
            double max = data[^1];

            Console.WriteLine($"--- {name} ---");
            Console.WriteLine($"Avg:    {avg:F2}");
            Console.WriteLine($"StdDev: {stdDev:F2}");
            Console.WriteLine($"Median: {median:F2}");
            Console.WriteLine($"95th %: {p95:F2}");
            Console.WriteLine($"99th %: {p99:F2}");
            Console.WriteLine($"Max:    {max:F2}");
            Console.WriteLine();
        }

        private void SampleUntil(ISamplingStrategy3D sampler)
        {
            Vector3 lastAverage;
            Vector3 currentAverage;
            float angleDiff;

            // 1. Initial Batch
            sampler.Sample(samplesPerRun);
            currentAverage = sampler.GetAverageNormal();

            // 2. Convergence Loop
            do
            {
                lastAverage = currentAverage;

                int added = sampler.Sample(samplesPerRun);
                if (added == 0) break; // Should not happen with infinite samplers, but good safety

                currentAverage = sampler.GetAverageNormal();

                float angleRad = MathUtil.UnsignedUnitVectorAngularDifferenceFast(lastAverage, currentAverage);
                angleDiff = MathUtil.ToDegrees(angleRad);

                // Safety break for extremely difficult scenarios
                if (sampler.NormalHistory.Count > 100_000) break;

            } while (angleDiff > maxChange);
        }
    }
}