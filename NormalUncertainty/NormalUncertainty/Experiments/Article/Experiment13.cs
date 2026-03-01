using MyLibrary;
using NormalUncertainty.Estimators;
using NormalUncertainty.Samplers;
using NormalUncertainty.Samplers._3D;
using NormalUncertainty.Scenario;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace NormalUncertainty.Experiments.Article
{
    public class Experiment13
    {
        private class EvalResult
        {
            public int SamplesTaken;
            public double UfDegrees;
            public long ElapsedTicks;
        }

        public void Run(string baseOutputDir, string modelPath, int targetScenarioCount = 10_000, int batchSize = 200, float stopThreshold = 0.05f)
        {
            Console.WriteLine("--- Experiment 13: Sampling Strategy & Neural Benchmark ---");
            Console.WriteLine($"Target Scenarios: {targetScenarioCount:N0}");
            Console.WriteLine($"Batch size: {batchSize:N0}");
            Console.WriteLine($"Threshold: {stopThreshold:F4}");
            Console.WriteLine($"Model: {modelPath}\n");

            HaltonCache.Initialize(9, 105_000); // Pad slightly above 100k

            // Initialize the ONNX Neural Estimator (Thread-safe for inference)
            using var neuralEstimator = new NeuralUncertaintyEstimator3D(modelPath);

            // 1. Data Ingestion
            var files = Directory.GetFiles(baseOutputDir, "evaluations.csv", SearchOption.AllDirectories);
            List<Scenario3D> testScenarios = [];
            Random fileRand = new Random();
            double selectionProbability = 0.001;

            Console.Write("Extracting random subset...");
            foreach (var file in files)
            {
                if (testScenarios.Count >= targetScenarioCount) break;
                foreach (var line in File.ReadLines(file).Skip(1))
                {
                    if (testScenarios.Count >= targetScenarioCount) break;
                    if (string.IsNullOrWhiteSpace(line) || fileRand.NextDouble() > selectionProbability) continue;

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

                        testScenarios.Add(new Scenario3D(aMin, aMax, bMin, bMax, cMin, cMax).Normalized());
                    }
                    catch { /* Skip malformed */ }
                }
            }
            Console.WriteLine($" Extracted {testScenarios.Count:N0} scenarios.\n");

            // Data storage for results
            var mcBaselineResults = new ConcurrentBag<EvalResult>();
            var mcAdaptiveResults = new ConcurrentBag<(EvalResult Res, double Diff)>();
            var haltonAdaptiveResults = new ConcurrentBag<(EvalResult Res, double Diff)>();
            var cornerResults = new ConcurrentBag<(EvalResult Res, double Diff)>();
            var neuralResults = new ConcurrentBag<(EvalResult Res, double Diff)>();

            int processed = 0;

            Console.WriteLine("Running benchmarks...");

            // 2. Parallel Evaluation Loop
            Parallel.ForEach(testScenarios, (scenario) =>
            {
                int seed = Guid.NewGuid().GetHashCode();

                // A. Ground Truth: MC 100k
                var sw = Stopwatch.StartNew();
                var mcBase = new BasicSampler3D(scenario, new Random(seed));
                mcBase.Sample(100_000);
                float ufBase = CalculateUf(mcBase.NormalHistory);
                sw.Stop();
                var baseRes = new EvalResult { SamplesTaken = mcBase.NormalHistory.Count, UfDegrees = ToDeg(ufBase), ElapsedTicks = sw.ElapsedTicks };
                mcBaselineResults.Add(baseRes);

                // B. MC Adaptive
                sw.Restart();
                var mcAdapt = new BasicSampler3D(scenario, new Random(seed));
                mcAdapt.SampleUntilConvergence(batchSize, stopThreshold, 100_000);
                float ufMcAdapt = CalculateUf(mcAdapt.NormalHistory);
                sw.Stop();
                mcAdaptiveResults.Add((new EvalResult { SamplesTaken = mcAdapt.NormalHistory.Count, UfDegrees = ToDeg(ufMcAdapt), ElapsedTicks = sw.ElapsedTicks }, Math.Abs(ToDeg(ufMcAdapt) - baseRes.UfDegrees)));

                // C. Halton Adaptive
                sw.Restart();
                var haltonAdapt = new CachedHaltonSampler3D(scenario);
                haltonAdapt.SampleUntilConvergence(batchSize, stopThreshold, 100_000);
                float ufHalton = CalculateUf(haltonAdapt.NormalHistory);
                sw.Stop();
                haltonAdaptiveResults.Add((new EvalResult { SamplesTaken = haltonAdapt.NormalHistory.Count, UfDegrees = ToDeg(ufHalton), ElapsedTicks = sw.ElapsedTicks }, Math.Abs(ToDeg(ufHalton) - baseRes.UfDegrees)));

                // D. Corners
                sw.Restart();
                var corners = new CornerSampler3D(scenario);
                corners.Sample(512);
                float ufCorner = CalculateUf(corners.NormalHistory);
                sw.Stop();
                cornerResults.Add((new EvalResult { SamplesTaken = corners.NormalHistory.Count, UfDegrees = ToDeg(ufCorner), ElapsedTicks = sw.ElapsedTicks }, Math.Abs(ToDeg(ufCorner) - baseRes.UfDegrees)));

                // E. Neural Estimator
                sw.Restart();
                float ufNeural = neuralEstimator.Estimate(scenario);
                sw.Stop();
                neuralResults.Add((new EvalResult { SamplesTaken = 1, UfDegrees = ToDeg(ufNeural), ElapsedTicks = sw.ElapsedTicks }, Math.Abs(ToDeg(ufNeural) - baseRes.UfDegrees)));

                int c = Interlocked.Increment(ref processed);
                if (c % 500 == 0) Console.Write(".");
            });

            Console.WriteLine("\n\nCalculating Statistics...\n");

            // 3. Print Tables
            PrintMethodStats("MC Baseline (100k)", mcBaselineResults.Select(r => (r, 0.0)));
            PrintMethodStats("MC Adaptive", mcAdaptiveResults);
            PrintMethodStats("Halton Adaptive", haltonAdaptiveResults);
            PrintMethodStats("Corners Fixed", cornerResults);
            PrintMethodStats("Neural Estimator", neuralResults);
        }

        private void PrintMethodStats(string name, IEnumerable<(EvalResult Res, double Diff)> data)
        {
            var list = data.ToList();
            var samples = list.Select(x => (double)x.Res.SamplesTaken).OrderBy(x => x).ToArray();
            var diffs = list.Select(x => x.Diff).OrderBy(x => x).ToArray();

            long totalTicks = list.Sum(x => x.Res.ElapsedTicks);
            double totalSamples = list.Sum(x => x.Res.SamplesTaken);

            double msPerScenario = (totalTicks / (double)Stopwatch.Frequency * 1000.0) / list.Count;
            double msPer1kSamples = (totalTicks / (double)Stopwatch.Frequency * 1000.0) / (totalSamples / 1000.0);

            Console.WriteLine($"=== {name} ===");
            Console.WriteLine($"{"Metric",-15} | {"Min",-8} | {"Avg",-8} | {"Median",-8} | {"95th",-8} | {"99th",-8} | {"Max",-8}");
            Console.WriteLine(new string('-', 75));

            // Samples
            Console.WriteLine($"{"Samples",-15} | {samples[0],-8:N0} | {samples.Average(),-8:N0} | {P(samples, 0.50),-8:N0} | {P(samples, 0.95),-8:N0} | {P(samples, 0.99),-8:N0} | {samples[^1],-8:N0}");

            // Differences
            if (name.Contains("Baseline"))
                Console.WriteLine($"{"Error (Δ°)",-15} | {"-",-8} | {"-",-8} | {"-",-8} | {"-",-8} | {"-",-8} | {"-",-8}");
            else
                Console.WriteLine($"{"Error (Δ°)",-15} | {diffs[0],-8:F4} | {diffs.Average(),-8:F4} | {P(diffs, 0.50),-8:F4} | {P(diffs, 0.95),-8:F4} | {P(diffs, 0.99),-8:F4} | {diffs[^1],-8:F4}");

            // For neural network, ms/1k samples doesn't make logical sense, so we hide it cleanly
            if (name.Contains("Neural"))
                Console.WriteLine($"\nPerformance: {msPerScenario:F4} ms / scenario\n");
            else
                Console.WriteLine($"\nPerformance: {msPerScenario:F4} ms / scenario  ||  {msPer1kSamples:F4} ms / 1,000 samples\n");
        }

        private double P(double[] sorted, double percentile)
        {
            int index = (int)Math.Ceiling(percentile * sorted.Length) - 1;
            return sorted[Math.Max(0, Math.Min(sorted.Length - 1, index))];
        }

        private float CalculateUf(List<Vector3> history)
        {
            if (history.Count == 0) return 0;
            Vector3 sum = Vector3.Zero;
            foreach (var n in history) sum += n;
            Vector3 mean = Vector3.Normalize(sum);

            double sumSqAngles = 0;
            foreach (var n in history)
            {
                float dot = Math.Clamp(Vector3.Dot(n, mean), -1f, 1f);
                sumSqAngles += Math.Pow(Math.Acos(dot), 2);
            }
            return (float)Math.Sqrt(sumSqAngles / history.Count);
        }

        private double ToDeg(float rad) => rad * (180.0 / Math.PI);
        private float Parse(string value) => float.Parse(value, CultureInfo.InvariantCulture);
    }
}