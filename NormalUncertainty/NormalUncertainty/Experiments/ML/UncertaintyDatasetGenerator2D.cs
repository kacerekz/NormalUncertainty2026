using MyLibrary;
using NormalUncertainty.Experiments.Convergence._2D;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;

namespace NormalUncertainty.Experiments.ML
{
    public class UncertaintyDatasetGenerator2D
    {
        private Random _r = new Random();
        private int _maxSamples = 100_000; // Ground Truth depth

        public void Generate(int datasetSize, string outputPath)
        {
            Console.WriteLine($"Generating {datasetSize} samples for ML training...");

            using (StreamWriter sw = new StreamWriter(outputPath))
            {
                // CSV Header
                // Inputs: A_height, B_min_x, B_min_y, B_max_x, B_max_y
                // Output: U_f (Normal Uncertainty)
                sw.WriteLine("A_height,B_min_x,B_min_y,B_max_x,B_max_y,U_f");

                for (int i = 0; i < datasetSize; i++)
                {
                    // 1. Generate Random Scenario
                    Scenario2D raw = new Scenario2D(_r);

                    // 2. Normalize Geometry
                    // Goal: A_min is (0,0), A_width is 1.0
                    Scenario2D normalized = Normalize(raw);

                    // 3. Extract Features (The 5 inputs for the Neural Net)
                    float aHeight = normalized.boundsAMax.Y - normalized.boundsAMin.Y;
                    Vector2 bMin = normalized.boundsBMin;
                    Vector2 bMax = normalized.boundsBMax;

                    // 4. Calculate Ground Truth U_f using Random Sampler
                    float uf = CalculateNormalUncertainty(normalized);

                    // 5. Write to CSV
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "{0},{1},{2},{3},{4},{5}",
                        aHeight,
                        bMin.X, bMin.Y,
                        bMax.X, bMax.Y,
                        uf));

                    if ((i + 1) % 1_000 == 0) Console.Write(".");
                }
            }
            Console.WriteLine($"\nDataset saved to {outputPath}");
        }

        private Scenario2D Normalize(Scenario2D raw)
        {
            // 1. Calculate Shift (to move A_min to 0,0)
            Vector2 shift = -raw.boundsAMin;

            // 2. Calculate Scale (to make A_width = 1.0)
            float rawWidth = raw.boundsAMax.X - raw.boundsAMin.X;
            float scale = 1.0f / rawWidth;

            // 3. Apply to A (We know A_min becomes 0,0 and A_max.X becomes 1.0)
            float newHeight = (raw.boundsAMax.Y - raw.boundsAMin.Y) * scale;
            Vector2 newAMin = Vector2.Zero;
            Vector2 newAMax = new Vector2(1.0f, newHeight);

            // 4. Apply to B
            Vector2 newBMin = (raw.boundsBMin + shift) * scale;
            Vector2 newBMax = (raw.boundsBMax + shift) * scale;

            // We use the private constructor via a helper or reflection, 
            // but since we are inside the logic, let's just assume we can create one.
            // *Note*: You might need to add a public constructor to Scenario2D 
            // that accepts explicit bounds, or add a 'SetBounds' method.
            // For now, assuming a constructor exists:

            return new Scenario2D(newAMin, newAMax, newBMin, newBMax);
        }

        private float CalculateNormalUncertainty(Scenario2D scenario)
        {
            // Ground Truth: Random Sampler
            BasicSampler2D sampler = new BasicSampler2D(scenario, _r);
            sampler.Sample(_maxSamples);

            List<Vector2> history = sampler.NormalHistory;
            if (history.Count == 0) return 0;

            // 1. Calculate Average Normal (n_bar)
            Vector2 sum = Vector2.Zero;
            foreach (var n in history) sum += n;
            Vector2 nBar = Vector2.Normalize(sum);

            // 2. Calculate U_f (Root Mean Square of Angle Differences)
            // Formula: Sqrt( (1/N) * Sum( arccos(n_j dot n_bar)^2 ) )
            double sumSqAngles = 0;

            foreach (var n in history)
            {
                // Dot product clamped to [-1, 1] to avoid NaN
                float dot = Math.Clamp(Vector2.Dot(n, nBar), -1f, 1f);

                // Angle in Radians
                double angle = Math.Acos(dot);

                sumSqAngles += (angle * angle);
            }

            return (float)Math.Sqrt(sumSqAngles / history.Count);
        }
    }
}