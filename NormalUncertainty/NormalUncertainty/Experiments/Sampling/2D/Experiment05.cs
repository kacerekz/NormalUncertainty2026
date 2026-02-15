using MyLibrary;
using System;
using System.Diagnostics;
using System.Numerics;

namespace NormalUncertainty.Experiments.Convergence._2D
{
    public class Experiment05
    {
        public void Run()
        {
            int scenarios = 10_000;
            int samplesPerRun = 100;
            float threshold = 0.01f;
            Random r = new Random();

            Stopwatch sw = new Stopwatch();

            // 1. Measure Random Speed
            sw.Start();
            for (int i = 0; i < scenarios; i++)
                RunConvergence(new BasicSampler2D(new Scenario2D(r), r), samplesPerRun, threshold);
            sw.Stop();
            long randomTime = sw.ElapsedMilliseconds;

            // 2. Measure Cached Halton Speed
            sw.Restart();
            for (int i = 0; i < scenarios; i++)
                RunConvergence(new CachedHaltonSampler2D(new Scenario2D(r)), samplesPerRun, threshold);
            sw.Stop();
            long haltonTime = sw.ElapsedMilliseconds;

            Console.WriteLine($"--- Experiment 05: Final Optimization Results ---");
            Console.WriteLine($"scenarioCount: {scenarios:N0}");
            Console.WriteLine($"samplesPerRun: {samplesPerRun:N0}");
            Console.WriteLine($"threshold:     {threshold:F6}");
            Console.WriteLine($"Random (MC) Total Time: {randomTime}ms");
            Console.WriteLine($"Cached Halton (QMC) Total Time: {haltonTime}ms");
            Console.WriteLine($"Net Speedup: {(double)randomTime / haltonTime:F2}x faster");
        }

        private void RunConvergence(ISamplingStrategy2D s, int batch, float limit)
        {
            // Internal logic similar to Experiment 03
            s.Sample(batch);
            Vector2 cur = s.GetAverageNormal();
            float diff;
            do
            {
                Vector2 prev = cur;
                s.Sample(batch);
                cur = s.GetAverageNormal();
                diff = MathUtil.ToDegrees(MathUtil.UnsignedUnitVectorAngularDifferenceFast(prev, cur));
            } while (diff > limit);
        }
    }
}