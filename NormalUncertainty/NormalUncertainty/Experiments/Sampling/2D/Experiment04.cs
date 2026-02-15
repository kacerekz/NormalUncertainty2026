using MyLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace NormalUncertainty.Experiments.Convergence._2D
{
    public class Experiment04
    {
        private int scenarioCount = 10_000;
        private int maxSamples = 10_000;
        private Random r = new Random();

        public void Run()
        {
            Console.WriteLine("--- Experiment 04: Execution Time (Halton vs Random) ---");
            Console.WriteLine($"scenarioCount: {scenarioCount:N0}");
            Console.WriteLine($"maxSamples:    {maxSamples:N0}");
            Console.WriteLine();

            Stopwatch sw = new Stopwatch();

            // --- 1. Measure Random Performance ---
            sw.Start();
            for (int i = 0; i < scenarioCount; i++)
            {
                Scenario2D scenario = new(r);
                ISamplingStrategy2D sampler = new BasicSampler2D(scenario, r);
                sampler.Sample(maxSamples);
            }
            sw.Stop();
            TimeSpan randomTime = sw.Elapsed;
            sw.Reset();

            // --- 2. Measure Halton Performance ---
            sw.Start();
            for (int i = 0; i < scenarioCount; i++)
            {
                Scenario2D scenario = new(r);
                ISamplingStrategy2D sampler = new HaltonSampler2D(scenario);
                sampler.Sample(maxSamples);
            }
            sw.Stop();
            TimeSpan haltonTime = sw.Elapsed;

            // --- Results ---
            Console.WriteLine($"--- Total Execution Time (for {scenarioCount:N0} scenarios) ---");
            Console.WriteLine($"Random: {randomTime.TotalSeconds:F2} seconds");
            Console.WriteLine($"Halton: {haltonTime.TotalSeconds:F2} seconds");
            Console.WriteLine();

            double ratio = haltonTime.TotalMilliseconds / randomTime.TotalMilliseconds;
            Console.WriteLine($"Halton is {ratio:F2}x slower per sample than Random.");

            Console.WriteLine("\n--- Conclusion ---");
            if (ratio > 2.0)
            {
                Console.WriteLine("Halton is so much slower that even requiring 50% fewer samples results in a NET LOSS in performance.");
            }
            else
            {
                Console.WriteLine("Halton's algorithmic overhead is low enough that its 2x convergence advantage makes it the faster choice overall.");
            }
        }
    }
}