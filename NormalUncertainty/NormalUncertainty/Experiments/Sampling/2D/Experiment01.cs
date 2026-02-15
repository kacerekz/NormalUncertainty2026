using MyLibrary;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NormalUncertainty.Experiments.Convergence._2D
{
    public class Experiment01
    {
        private int scenarioCount = 10_000;
        private int maxSamples = 100_000;
        private int samplesPerRun = 3000;
        private float maxChange = 0.01f; // degrees

        private Random r = new Random();

        public void Run()
        {
            Console.WriteLine("--- Experiment 01: Random vs Early Stop Random ---"); 
            Console.WriteLine($"scenarioCount: {scenarioCount:N0}");
            Console.WriteLine($"maxSamples:    {maxSamples:N0}");
            Console.WriteLine($"samplesPerRun: {samplesPerRun:N0}");
            Console.WriteLine($"maxChange:     {maxChange:F6}");
            Console.WriteLine();

            int maxEarlySamples = 0;
            double sumEarlySamples = 0;
            double sumSqEarlySamples = 0;

            float maxDifference = 0;
            double sumDifference = 0;
            double sumSqDifference = 0;

            List<float> angleHistory = [];
            List<int> sampleHistory = [];

            for (int i = 0; i < scenarioCount; i++)
            {
                Scenario2D scenario2D = new(r);
                BasicSampler2D sampler2D = new(scenario2D, r);

                Scenario2D clone = scenario2D.Clone();
                BasicSampler2D cloneSampler = new(clone, r);

                SampleUntil(sampler2D, samplesPerRun, maxChange);
                cloneSampler.Sample(maxSamples);

                Vector2 averageEarly = sampler2D.GetAverageNormal();
                Vector2 averageAll = cloneSampler.GetAverageNormal();

                float angle = MathUtil.UnsignedUnitVectorAngularDifferenceFast(averageEarly, averageAll);
                angle = MathUtil.ToDegrees(angle);
                angleHistory.Add(angle);

                // Angular Difference Stats
                maxDifference = Math.Max(maxDifference, angle);
                sumDifference += angle;
                sumSqDifference += (angle * angle); 

                // Early Stop Stats 
                int samples = sampler2D.NormalHistory.Count;
                maxEarlySamples = Math.Max(samples, maxEarlySamples);
                sumEarlySamples += samples;
                sumSqEarlySamples += (double)samples * samples;
                sampleHistory.Add(samples);
            }

            // Angular Difference Stats
            double avgDifference = sumDifference / scenarioCount;
            double stdDevDifference = Math.Sqrt((sumSqDifference - (sumDifference * sumDifference) / scenarioCount) / (scenarioCount - 1));

            angleHistory.Sort();
            float median = angleHistory[angleHistory.Count / 2];
            float p95 = angleHistory[(int)(angleHistory.Count * 0.95)];
            float p99 = angleHistory[(int)(angleHistory.Count * 0.99)];

            // Early Stop Stats 
            double avgEarlySamples = sumEarlySamples / scenarioCount;
            double stdDevEarlySamples = Math.Sqrt((sumSqEarlySamples - (sumEarlySamples * sumEarlySamples) / scenarioCount) / (scenarioCount - 1));

            sampleHistory.Sort();
            float medianS = sampleHistory[sampleHistory.Count / 2];
            float p95S = sampleHistory[(int)(sampleHistory.Count * 0.95)];
            float p99S = sampleHistory[(int)(sampleHistory.Count * 0.99)];

            Console.WriteLine($"--- Angular Difference [Degrees] ---");
            Console.WriteLine($"Max: {maxDifference:F6}");
            Console.WriteLine($"Avg: {avgDifference:F6}");
            Console.WriteLine($"StdDev: {stdDevDifference:F6}");
            Console.WriteLine($"Median: {median:F6}");
            Console.WriteLine($"95th %: {p95:F6}");   
            Console.WriteLine($"99th %: {p99:F6}");   

            Console.WriteLine($"\n--- Early Stop Samples ---");
            Console.WriteLine($"Max: {maxEarlySamples}");
            Console.WriteLine($"Avg: {avgEarlySamples:F2}");
            Console.WriteLine($"StdDev: {stdDevEarlySamples:F2}");
            Console.WriteLine($"Median: {medianS:F2}");
            Console.WriteLine($"95th %: {p95S:F2}");
            Console.WriteLine($"99th %: {p99S:F2}");
        }

        private void SampleUntil(ISamplingStrategy2D sampler, int samplesPerRun, float maxChangeDegrees)
        {
            Vector2 lastAverage;
            Vector2 currentAverage;
            float angleDiff;

            // 1. Initial Sample Batch
            sampler.Sample(samplesPerRun);
            currentAverage = sampler.GetAverageNormal();

            // 2. Convergence Loop
            do
            {
                lastAverage = currentAverage;

                // Add another batch
                int samplesAdded = sampler.Sample(samplesPerRun);

                // Safety check: if the sampler runs out of data (e.g. finite grid), stop early
                if (samplesAdded == 0) break;

                currentAverage = sampler.GetAverageNormal();

                // Calculate change in average normal
                float angleRad = MathUtil.UnsignedUnitVectorAngularDifferenceFast(lastAverage, currentAverage);
                angleDiff = MathUtil.ToDegrees(angleRad);
            }
            while (angleDiff > maxChangeDegrees);
        }
    }
}
