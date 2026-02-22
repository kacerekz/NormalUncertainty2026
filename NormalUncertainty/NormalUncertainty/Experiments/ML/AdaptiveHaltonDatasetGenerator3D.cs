using MyLibrary;
using NormalUncertainty.Experiments.Convergence._3D;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;

namespace NormalUncertainty.Experiments.ML
{
    public class AdaptiveHaltonDatasetGenerator3D
    {
        private Random _r = new Random();
        private const int MaxSafetySamples = 50_000;

        public void Generate(int datasetSize, string outputPath, int batchSize = 200, float thresholdDegrees = 0.05f)
        {
            Console.WriteLine($"Generating {datasetSize:N0} Adaptive scenarios...");
            Console.WriteLine($"Strategy: Batch {batchSize} | Stop if ΔMean < {thresholdDegrees}°");
            Console.WriteLine($"Output: {outputPath} (Shared Read Access Enabled)");

            // Ensure Halton Cache is large enough
            HaltonCache.Initialize(9, MaxSafetySamples + 5000);

            // Open with FileShare.Read so you can copy/train on this file WHILE it is being written.
            using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var sw = new StreamWriter(fs, Encoding.UTF8))
            {
                // CSV Header
                sw.WriteLine("A_h,A_d,B_min_x,B_min_y,B_min_z,B_max_x,B_max_y,B_max_z,C_min_x,C_min_y,C_min_z,C_max_x,C_max_y,C_max_z,U_f");

                for (int i = 0; i < datasetSize; i++)
                {
                    // 1. Generate & Normalize
                    Scenario3D raw = new Scenario3D(_r);
                    Scenario3D normalized = raw.Normalized();

                    // 2. Adaptive Sampling
                    float uf = MeasureUncertaintyAdaptive(normalized, batchSize, thresholdDegrees);

                    // 3. Get Network Inputs
                    float[] f = normalized.GetNetworkInput();

                    // 4. Write to CSV
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14}",
                        f[0], f[1],
                        f[2], f[3], f[4], f[5], f[6], f[7],
                        f[8], f[9], f[10], f[11], f[12], f[13],
                        uf));

                    // 5. Progress & Flush
                    // Flush every 100 items so you can grab the data early without waiting for the buffer
                    if ((i + 1) % 1_000 == 0)
                    {
                        sw.Flush();
                        Console.Write(".");
                    }
                    if ((i + 1) % 100_000 == 0)
                    {
                        Console.WriteLine();
                    }
                }
            }
            Console.WriteLine($"\nAdaptive Dataset saved to {outputPath}");
        }

        private float MeasureUncertaintyAdaptive(Scenario3D s, int batchSize, float thresholdDeg)
        {
            var sampler = new CachedHaltonSampler3D(s);

            Vector3 prevMean = Vector3.Zero;
            int totalSamples = 0;

            while (totalSamples < MaxSafetySamples)
            {
                int added = sampler.Sample(batchSize);
                totalSamples += batchSize;

                if (sampler.NormalHistory.Count == 0) continue;

                Vector3 currentMean = sampler.GetAverageNormal();

                if (totalSamples > batchSize)
                {
                    float angleRad = MathUtil.UnsignedUnitVectorAngularDifferenceFast(prevMean, currentMean);
                    float angleDeg = MathUtil.ToDegrees(angleRad);

                    if (angleDeg < thresholdDeg) break;
                }
                prevMean = currentMean;
            }

            return CalculateUf(sampler.NormalHistory);
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
                double angle = Math.Acos(dot);
                sumSqAngles += (angle * angle);
            }

            return (float)Math.Sqrt(sumSqAngles / history.Count);
        }
    }
}