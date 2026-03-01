using NormalUncertainty.Estimators;
using NormalUncertainty.Samplers._3D;
using NormalUncertainty.Scenario;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NormalUncertainty.Experiments.Article
{
    public class Experiment10
    {
        public void Run(string baseOutputDir, int targetScenarioCount = 100_000, int monteCarloSamples = 100_000)
        {
            Console.WriteLine("--- Experiment 10: Scale-Invariance Validation ---");
            Console.WriteLine($"Scanning: {baseOutputDir}");
            Console.WriteLine($"Target Scenarios: {targetScenarioCount:N0}");
            Console.WriteLine($"Monte Carlo Samples per Scenario: {monteCarloSamples:N0}\n");

            var files = Directory.GetFiles(baseOutputDir, "evaluations.csv", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                Console.WriteLine("No evaluations.csv files found.");
                return;
            }

            // 1. Memory-Efficient Lazy Loading & Random Sub-sampling
            List<Scenario3D> testScenarios = [];
            Random fileRand = new Random();

            // To fairly sample across 5GB+, we pick scenarios with a low probability
            // adjusting dynamically is tricky without knowing line counts, so we iterate
            // through files and grab lines probabilistically until full.
            double selectionProbability = 0.05; // 5% chance to pick a line

            Console.Write("Extracting random subset from 5GB+ dataset...");
            foreach (var file in files)
            {
                if (testScenarios.Count >= targetScenarioCount) break;

                // ReadLines does NOT load the whole file into RAM
                foreach (var line in File.ReadLines(file).Skip(1))
                {
                    if (testScenarios.Count >= targetScenarioCount) break;
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (fileRand.NextDouble() > selectionProbability) continue;

                    var p = line.Split([';', ','], StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length < 19) continue;

                    try
                    {
                        Vector3 aMin = new(Parse(p[0]), Parse(p[2]), Parse(p[4]));
                        Vector3 aMax = new(Parse(p[1]), Parse(p[3]), Parse(p[5]));
                        Vector3 bMin = new(Parse(p[6]), Parse(p[8]), Parse(p[10]));
                        Vector3 bMax = new(Parse(p[7]), Parse(p[9]), Parse(p[11]));
                        Vector3 cMin = new(Parse(p[12]), Parse(p[14]), Parse(p[16]));
                        Vector3 cMax = new(Parse(p[13]), Parse(p[15]), Parse(p[17]));

                        testScenarios.Add(new Scenario3D(aMin, aMax, bMin, bMax, cMin, cMax));
                    }
                    catch (Exception) { /* Skip malformed */ }
                }
            }
            Console.WriteLine($" Extracted {testScenarios.Count:N0} scenarios.\n");

            // 2. Parallel Evaluation Loop
            ConcurrentBag<double> absoluteDifferences = [];
            int processed = 0;
            long startTicks = Stopwatch.GetTimestamp();

            Console.WriteLine("Evaluating U_f (Raw vs Normalized)...");

            Parallel.ForEach(testScenarios, (rawScenario) =>
            {
                // Create the normalized clone
                Scenario3D normScenario = rawScenario.Normalized();

                // Synchronize the random seeds to eliminate Monte Carlo variance
                int sharedSeed = Guid.NewGuid().GetHashCode();

                var estRaw = new SamplingUncertaintyEstimator3D(
                    s => new BasicSampler3D(s, new Random(sharedSeed)),
                    sampler => sampler.Sample(monteCarloSamples)
                );

                var estNorm = new SamplingUncertaintyEstimator3D(
                    s => new BasicSampler3D(s, new Random(sharedSeed)),
                    sampler => sampler.Sample(monteCarloSamples)
                );

                // Estimate both
                float ufRaw = estRaw.Estimate(rawScenario);
                float ufNorm = estNorm.Estimate(normScenario);

                // Record the absolute difference in Degrees
                double diffDeg = Math.Abs(ufRaw - ufNorm) * (180.0 / Math.PI);
                absoluteDifferences.Add(diffDeg);

                int currentCount = Interlocked.Increment(ref processed);
                if (currentCount % 1000 == 0) Console.Write(".");
            });

            // 3. Statistical Validation Output
            double elapsedSec = (Stopwatch.GetTimestamp() - startTicks) / (double)Stopwatch.Frequency;
            Console.WriteLine($"\n\nEvaluation completed in {elapsedSec:F2} seconds.");

            var diffs = absoluteDifferences.ToList();
            diffs.Sort();

            double maxDiff = diffs[^1];
            double avgDiff = diffs.Average();
            double sumSq = diffs.Sum(d => Math.Pow(d - avgDiff, 2));
            double stdDev = Math.Sqrt(sumSq / Math.Max(1, diffs.Count - 1));

            Console.WriteLine("\n--- Scale-Invariance Proof [Degrees] ---");
            Console.WriteLine($"Average Disparity: {avgDiff:E4}°");
            Console.WriteLine($"StdDev Disparity:  {stdDev:E4}°");
            Console.WriteLine($"Max Disparity:     {maxDiff:E4}°");

            if (maxDiff < 1e-4)
            {
                Console.WriteLine("\nSUCCESS: The metric is mathematically scale-invariant.");
                Console.WriteLine("Disparities are entirely within the margin of floating-point precision error.");
            }
            if (maxDiff < 1e-2)
            {
                Console.WriteLine("\nSUCCESS: The metric is mathematically scale-invariant.");
                Console.WriteLine("There may be additional benefit to normalization due to floating-point arithmetic for far-from-origin scenarios.");
            }
            else
            {
                Console.WriteLine("\nWARNING: Disparity exceeds floating point error. Check normalization math.");
            }
        }

        private float Parse(string value) => float.Parse(value, CultureInfo.InvariantCulture);
    }
}
