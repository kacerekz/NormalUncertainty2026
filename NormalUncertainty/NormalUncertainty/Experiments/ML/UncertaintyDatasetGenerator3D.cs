using MyLibrary;
using NormalUncertainty.Experiments.Convergence._3D;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;

namespace NormalUncertainty.Experiments.ML
{
    public class UncertaintyDatasetGenerator3D
    {
        private Random _r = new Random();
        // We use the Cached Halton sampler because it's validated & fast
        // 100k samples is overkill for training data; 5k-10k per scenario is usually enough for a stable mean
        // But let's stick to your "Convergence" logic to be safe.
        private const int SamplesPerScenario = 5000;

        public void Generate(int datasetSize, string outputPath)
        {
            Console.WriteLine($"Generating {datasetSize} 3D scenarios for ML training...");

            // Ensure Cache is ready
            HaltonCache3D.Initialize();

            using (StreamWriter sw = new StreamWriter(outputPath))
            {
                // CSV Header (14 Inputs + 1 Output)
                sw.WriteLine("A_h,A_d,B_min_x,B_min_y,B_min_z,B_max_x,B_max_y,B_max_z,C_min_x,C_min_y,C_min_z,C_max_x,C_max_y,C_max_z,U_f");

                for (int i = 0; i < datasetSize; i++)
                {
                    // 1. Generate Random 3D Scenario
                    Scenario3D raw = new Scenario3D(_r);

                    // 2. Normalize (A_width becomes 1.0)
                    (Vector3 aSize, Vector3 bMin, Vector3 bMax, Vector3 cMin, Vector3 cMax) = Normalize(raw);

                    // 3. Ground Truth U_f Calculation
                    // We run the simulation on the NORMALIZED scenario to match the inputs
                    // (Actually, U_f is scale-invariant, so running on 'raw' gives the same result, 
                    // but let's run on raw to avoid floating point drift in the sampler).
                    float uf = CalculateNormalUncertainty(raw);

                    // 4. Write to CSV
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14}",
                        aSize.Y, aSize.Z, // A inputs (Height, Depth)
                        bMin.X, bMin.Y, bMin.Z, bMax.X, bMax.Y, bMax.Z, // B inputs
                        cMin.X, cMin.Y, cMin.Z, cMax.X, cMax.Y, cMax.Z, // C inputs
                        uf)); // Target

                    if ((i + 1) % 1000 == 0) Console.Write(".");
                }
            }
            Console.WriteLine($"\n3D Dataset saved to {outputPath}");
        }

        private (Vector3 aSize, Vector3 bMin, Vector3 bMax, Vector3 cMin, Vector3 cMax) Normalize(Scenario3D s)
        {
            // Shift so A_min is at (0,0,0)
            Vector3 shift = -s.BoundsAMin;

            // Scale so A_width (X) is 1.0
            float scale = 1.0f / (s.BoundsAMax.X - s.BoundsAMin.X);

            // Calculate Normalized Dimensions
            Vector3 aSize = (s.BoundsAMax - s.BoundsAMin) * scale; // X is now 1.0

            Vector3 bMin = (s.BoundsBMin + shift) * scale;
            Vector3 bMax = (s.BoundsBMax + shift) * scale;

            Vector3 cMin = (s.BoundsCMin + shift) * scale;
            Vector3 cMax = (s.BoundsCMax + shift) * scale;

            return (aSize, bMin, bMax, cMin, cMax);
        }

        private float CalculateNormalUncertainty(Scenario3D s)
        {
            // Use your optimized sampler
            var sampler = new CachedHaltonSampler3D(s);
            sampler.Sample(SamplesPerScenario);

            List<Vector3> history = sampler.NormalHistory;
            if (history.Count == 0) return 0;

            // 1. Compute Mean Normal
            Vector3 sum = Vector3.Zero;
            foreach (var n in history) sum += n;
            Vector3 mean = Vector3.Normalize(sum);

            // 2. Compute RMS of Angles
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