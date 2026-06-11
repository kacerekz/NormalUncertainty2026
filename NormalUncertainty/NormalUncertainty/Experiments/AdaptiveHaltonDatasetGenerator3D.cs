using MyLibrary;
using NormalUncertainty.Estimators;
using NormalUncertainty.Samplers._3D;
using NormalUncertainty.Samplers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using NormalUncertainty.Scenario;

namespace NormalUncertainty.Experiments
{
    public class AdaptiveHaltonDatasetGenerator3D
    {
        private const int MaxSafetySamples = 50_000;

        public void Generate(int datasetSize, string outputPath, int batchSize = 200, float thresholdDegrees = 0.01f)
        {
            Console.WriteLine($"Generating {datasetSize:N0} Adaptive scenarios...");
            Console.WriteLine($"Strategy: Batch {batchSize} | Stop if ΔMean < {thresholdDegrees}°");
            Console.WriteLine($"Output: {outputPath} (Shared Read Access Enabled)");

            // 1. Initialize Cache
            HaltonCache.Initialize(9, MaxSafetySamples + 5000);

            // 2. Setup the Estimator (Strategy Pattern)
            var estimator = new SamplingUncertaintyEstimator3D(
                s => new CachedHaltonSampler3D(s),
                sampler => sampler.SampleUntilConvergence(batchSize, thresholdDegrees, MaxSafetySamples)
            );

            // 3. Setup the Native Normalized Generator (Fixing the Scaling Skew)
            var config = new GenerationConfig
            {
                MinSize = 0.01f,
                MaxSize = 10.0f,
                MinDistance = 0.01f,
                MaxDistance = 50.0f
            };
            var generator = new NormalizedScenarioGenerator(new Random(), config);

            // Open with FileShare.Read so you can copy/train on this file WHILE it is being written
             using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
             using (var sw = new StreamWriter(fs, Encoding.UTF8))
            {
                // CSV Header
                sw.WriteLine("A_w,A_h,A_d,B_min_x,B_min_y,B_min_z,B_max_x,B_max_y,B_max_z,C_min_x,C_min_y,C_min_z,C_max_x,C_max_y,C_max_z,U_f");

                for (int i = 0; i < datasetSize; i++)
                {
                    // A. Generate Natively Normalized Scenario
                    Scenario3D s = generator.Generate();

                    // B. Evaluate using the Strategy Pattern Estimator
                    float uf = estimator.Estimate(s);

                    // C. Extract Features
                    float[] f = s.GetNetworkInput();

                    // D. Write to CSV
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15}",
                        f[0], f[1], f[2], f[3], f[4], f[5], f[6], f[7],
                        f[8], f[9], f[10], f[11], f[12], f[13], f[14], uf));

                    // E. Progress & Flush
                    if ((i + 1) % 1_000 == 0)
                    {
                        sw.Flush();
                        Console.Write(".");
                    }
                    if ((i + 1) % 100_000 == 0) Console.WriteLine();
                }
            }

            Console.WriteLine($"\nAdaptive Dataset saved to {outputPath}");
        }
    }
}