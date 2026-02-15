using MyLibrary;
using NormalUncertainty.Experiments.ML;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace NormalUncertainty.Experiments.Convergence._3D
{
    public class Experiment09
    {
        private int scenarioCount = 10_000;
        private Random r = new Random();
        private const int HaltonGroundTruthSamples = 5000;

        public void Run()
        {
            Console.WriteLine("--- Experiment 09: Neural Net vs Corners vs Volume ---");
            Console.WriteLine($"Scenarios: {scenarioCount:N0}");
            Console.WriteLine();

            // Load the ONNX model (Make sure the path matches your output!)
            using var neuralNet = new NeuralUncertaintyEstimator3D("uncertainty_model_3d.onnx");

            List<double> cornerErrors = new();
            List<double> neuralErrors = new();

            List<double> haltonTimes = new();
            List<double> cornerTimes = new();
            List<double> neuralTimes = new();

            // Warmup to avoid JIT/Model load spikes in timing
            Warmup(neuralNet);

            for (int i = 0; i < scenarioCount; i++)
            {
                Scenario3D s = new Scenario3D(r);

                // --- 1. Ground Truth (Halton Volume) ---
                long t0 = Stopwatch.GetTimestamp();
                var haltonSampler = new CachedHaltonSampler3D(s);
                haltonSampler.Sample(HaltonGroundTruthSamples);
                float truthUf = CalculateUf(haltonSampler.NormalHistory);
                haltonTimes.Add(GetMs(t0));

                // --- 2. Corner Proxy (512) ---
                long t1 = Stopwatch.GetTimestamp();
                var cornerSampler = new CornerSampler3D(s);
                cornerSampler.Sample(512);
                float cornerUf = CalculateUf(cornerSampler.NormalHistory);
                cornerTimes.Add(GetMs(t1));

                // --- 3. Neural Net Proxy (ONNX) ---
                long t2 = Stopwatch.GetTimestamp();
                float neuralUf = neuralNet.PredictUf(s);
                neuralTimes.Add(GetMs(t2));

                // --- Measure Errors (Convert Radians to Degrees for readability) ---
                cornerErrors.Add(Math.Abs(truthUf - cornerUf) * (180.0 / Math.PI));
                neuralErrors.Add(Math.Abs(truthUf - neuralUf) * (180.0 / Math.PI));

                if ((i + 1) % 1000 == 0) Console.Write(".");
            }
            Console.WriteLine("\n");

            // --- Analysis ---
            Console.WriteLine(">>> Accuracy: Error in U_f [Degrees]");
            PrintComparisonStats("Corner Proxy Error", cornerErrors, "Neural Net Error", neuralErrors);

            Console.WriteLine(">>> Speed: Execution Time [ms]");
            PrintStats("Halton Volume (5000) [ms]", haltonTimes);
            PrintComparisonStats("Corner Proxy (512) [ms]", cornerTimes, "Neural Net (ONNX) [ms]", neuralTimes);

            // Conclusion
            double avgHalton = haltonTimes.Average();
            double avgNeural = neuralTimes.Average();
            Console.WriteLine(">>> Conclusion");
            Console.WriteLine($"Neural Net Speedup vs Volume:  {(avgHalton / avgNeural):F2}x faster");
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

        private double GetMs(long startTimestamp)
        {
            return (double)(Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
        }

        private void Warmup(NeuralUncertaintyEstimator3D net)
        {
            Scenario3D s = new Scenario3D(r);
            HaltonCache3D.Initialize();
            var h = new CachedHaltonSampler3D(s); h.Sample(100); CalculateUf(h.NormalHistory);
            var c = new CornerSampler3D(s); c.Sample(512); CalculateUf(c.NormalHistory);
            net.PredictUf(s);
        }

        private void PrintStats(string name, List<double> data)
        {
            data.Sort();
            Console.WriteLine($"--- {name} ---");
            Console.WriteLine($"Avg: {data.Average():F4} | Median: {data[data.Count / 2]:F4} | Max: {data[^1]:F4}\n");
        }

        private void PrintComparisonStats(string name1, List<double> data1, string name2, List<double> data2)
        {
            data1.Sort();
            data2.Sort();

            Console.WriteLine($"Metric      | {name1,-20} | {name2,-20}");
            Console.WriteLine($"------------|----------------------|----------------------");
            Console.WriteLine($"Average     | {data1.Average(),-20:F4} | {data2.Average(),-20:F4}");
            Console.WriteLine($"Median      | {data1[data1.Count / 2],-20:F4} | {data2[data2.Count / 2],-20:F4}");
            Console.WriteLine($"95th %      | {data1[(int)(data1.Count * 0.95)],-20:F4} | {data2[(int)(data2.Count * 0.95)],-20:F4}");
            Console.WriteLine($"99th %      | {data1[(int)(data1.Count * 0.99)],-20:F4} | {data2[(int)(data2.Count * 0.99)],-20:F4}");
            Console.WriteLine($"Max         | {data1[^1],-20:F4} | {data2[^1],-20:F4}");
            Console.WriteLine();
        }
    }
}