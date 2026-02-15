using MyLibrary;
using NormalUncertainty.Experiments.Convergence._3D;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;

namespace NormalUncertainty.Experiments.ML
{
    public class RandomUncertaintyDatasetGenerator3D
    {
        private Random _r = new Random();
        
        // "Max Random Samples" to establish a solid Ground Truth
        private const int MaxSamples = 100_000;

        public void GenerateParallel(int datasetSize, string outputPath)
        {
            Console.WriteLine($"Generating {datasetSize:N0} 3D scenarios (Parallel)...");

            // Progress counter (thread-safe)
            int progress = 0;
            object fileLock = new object();

            using (StreamWriter sw = new StreamWriter(outputPath))
            {
                // 1. Write Header
                sw.WriteLine("A_h,A_d,B_min_x,B_min_y,B_min_z,B_max_x,B_max_y,B_max_z,C_min_x,C_min_y,C_min_z,C_max_x,C_max_y,C_max_z,U_f");

                // 2. Partition the work into chunks to reduce locking overhead
                // This automatically groups iterations (e.g., batches of 100-1000)
                var rangePartitioner = Partitioner.Create(0, datasetSize);
                // New: Force chunks of 1,000 items
                //var rangePartitioner = Partitioner.Create(0, datasetSize, 10);

                Parallel.ForEach(rangePartitioner, (range) =>
                {
                    // --- Thread Local Setup ---
                    // Create a local Random instance for this specific thread/chunk
                    // (Using Guid hash ensures unique seeds across threads)
                    Random localR = new Random(Guid.NewGuid().GetHashCode());

                    // Local buffer to store lines before writing to disk
                    StringBuilder localBuffer = new StringBuilder();

                    // Loop through the assigned sub-range
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        // A. Generate with LOCAL Random
                        Scenario3D raw = new Scenario3D(localR);

                        // B. Normalize
                        (Vector3 aSize, Vector3 bMin, Vector3 bMax, Vector3 cMin, Vector3 cMax) = Normalize(raw);

                        // C. Calculate Ground Truth (Heavy Calculation)
                        float uf = CalculateNormalUncertainty(raw);

                        // D. Buffer the string (Do NOT write to file yet)
                        localBuffer.AppendFormat(CultureInfo.InvariantCulture,
                            "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14}\n",
                            aSize.Y, aSize.Z,
                            bMin.X, bMin.Y, bMin.Z, bMax.X, bMax.Y, bMax.Z,
                            cMin.X, cMin.Y, cMin.Z, cMax.X, cMax.Y, cMax.Z,
                            uf);
                    }

                    // --- Synchronization Point ---
                    // Only lock ONCE per chunk, instead of once per line.
                    lock (fileLock)
                    {
                        sw.Write(localBuffer.ToString()); // Batch write to file

                        // Update global progress
                        //int currentCount = Interlocked.Add(ref progress, range.Item2 - range.Item1);

                        // Simple check: Print a dot
                        //Console.Write(".");
                        //Console.Out.Flush(); // FORCE the dot to appear immediately
                    }
                });
            }

            Console.WriteLine($"\nDataset saved to {outputPath}");
        }

        public void Generate(int datasetSize, string outputPath)
        {
            Console.WriteLine($"Generating {datasetSize} 3D scenarios using Random Sampling (MC)...");
            Console.WriteLine($"Samples per Scenario: {MaxSamples:N0}");

            using (StreamWriter sw = new StreamWriter(outputPath))
            {
                // CSV Header (Matches the Halton one exactly for easy swapping)
                sw.WriteLine("A_h,A_d,B_min_x,B_min_y,B_min_z,B_max_x,B_max_y,B_max_z,C_min_x,C_min_y,C_min_z,C_max_x,C_max_y,C_max_z,U_f");

                for (int i = 0; i < datasetSize; i++)
                {
                    // 1. Generate Random 3D Scenario
                    Scenario3D raw = new Scenario3D(_r);

                    // 2. Normalize (A_width becomes 1.0)
                    (Vector3 aSize, Vector3 bMin, Vector3 bMax, Vector3 cMin, Vector3 cMax) = Normalize(raw);

                    // 3. Ground Truth U_f Calculation (Random Sampler)
                    // We run this on the RAW scenario to avoid floating point drift, result is scale-invariant.
                    float uf = CalculateNormalUncertainty(raw);

                    // 4. Write to CSV
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14}",
                        aSize.Y, aSize.Z, // A inputs
                        bMin.X, bMin.Y, bMin.Z, bMax.X, bMax.Y, bMax.Z, // B inputs
                        cMin.X, cMin.Y, cMin.Z, cMax.X, cMax.Y, cMax.Z, // C inputs
                        uf)); // Target

                    if ((i + 1) % 1_000 == 0) Console.Write(".");
                }
            }
            Console.WriteLine($"\nRandom (MC) Dataset saved to {outputPath}");
        }

        private (Vector3 aSize, Vector3 bMin, Vector3 bMax, Vector3 cMin, Vector3 cMax) Normalize(Scenario3D s)
        {
            Vector3 shift = -s.BoundsAMin;
            float scale = 1.0f / (s.BoundsAMax.X - s.BoundsAMin.X);

            Vector3 aSize = (s.BoundsAMax - s.BoundsAMin) * scale;
            Vector3 bMin = (s.BoundsBMin + shift) * scale;
            Vector3 bMax = (s.BoundsBMax + shift) * scale;
            Vector3 cMin = (s.BoundsCMin + shift) * scale;
            Vector3 cMax = (s.BoundsCMax + shift) * scale;

            return (aSize, bMin, bMax, cMin, cMax);
        }

        private float CalculateNormalUncertainty(Scenario3D s)
        {
            // Use BasicSampler3D (Random / Monte Carlo)
            var sampler = new BasicSampler3D(s, _r);
            sampler.Sample(MaxSamples);

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