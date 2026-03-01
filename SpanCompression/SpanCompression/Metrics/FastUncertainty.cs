using SpanCompression.ML;
using SpanCompression.Structures;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace SpanCompression
{
    public static class FastUncertainty
    {
        // Strategy 1: Cached Halton (O(N) Streaming)
        // This replaces the nested loops with a single loop of 'sampleCount'
        public static double CalculateHalton(Cell c1, Cell c2, Cell c3, int sampleCount = 100)
        {
            // We accumulate the sum of squared angles
            double sumSqAngles = 0;
            Vector3 sumNormal = Vector3.Zero;
            List<Vector3> history = new List<Vector3>(sampleCount);

            // 1. Stream Samples & Compute Normals
            // We use a different offset for each cell's dimensions to ensure independence
            // Cell 1: Dims 0,1,2 | Cell 2: Dims 3,4,5 | Cell 3: Dims 6,7,8
            for (int i = 0; i < sampleCount; i++)
            {
                Vector3 p1 = GetHaltonPoint(c1, i, 0);
                Vector3 p2 = GetHaltonPoint(c2, i, 3);
                Vector3 p3 = GetHaltonPoint(c3, i, 6);

                Vector3 u = p2 - p1;
                Vector3 v = p3 - p1;
                Vector3 normal = Vector3.Cross(u, v); // Not normalized yet for speed? No, need normalize for Sum

                if (normal.LengthSquared() > 1e-9f)
                {
                    normal = Vector3.Normalize(normal);
                    sumNormal += normal;
                    history.Add(normal);
                }
            }

            if (history.Count == 0) return 0;

            // 2. Average Normal
            Vector3 meanNormal = Vector3.Normalize(sumNormal);

            // 3. Variance Calculation
            foreach (var n in history)
            {
                float dot = Math.Clamp(Vector3.Dot(n, meanNormal), -1f, 1f);
                double angle = Math.Acos(dot);
                sumSqAngles += angle * angle;
            }

            return Math.Sqrt(sumSqAngles / history.Count);
        }

        // Strategy 2: Neural Inference (O(1) Instant)
        //public static double CalculateNeural(Cell c1, Cell c2, Cell c3, NeuralUncertaintyEstimator brain)
        //{
        //    // Neural net returns radians directly
        //    return brain.Predict(c1, c2, c3);
        //}

        private static Vector3 GetHaltonPoint(Cell c, int index, int dimOffset)
        {
            float tx = HaltonSequence.Get(index, dimOffset);
            float ty = HaltonSequence.Get(index, dimOffset + 1);
            float tz = HaltonSequence.Get(index, dimOffset + 2);

            return new Vector3(
                c.SpanX.Min + tx * c.SpanX.Size,
                c.SpanY.Min + ty * c.SpanY.Size,
                c.SpanZ.Min + tz * c.SpanZ.Size
            );
        }

        public static double CalculateAdaptiveHalton(Cell c1, Cell c2, Cell c3, int batchSize = 200, double stopThresholdDeg = 0.05, int maxSamples = 5000)
        {
            double stopThresholdRad = stopThresholdDeg * (Math.PI / 180.0);
            Vector3 sumNormal = Vector3.Zero;
            Vector3 prevMeanNormal = Vector3.Zero;

            // Pre-allocate to avoid GC resizing during evaluation
            List<Vector3> history = new List<Vector3>(maxSamples);

            int samplesEvaluated = 0;

            while (samplesEvaluated < maxSamples)
            {
                int currentBatch = Math.Min(batchSize, maxSamples - samplesEvaluated);

                // 1. Evaluate Batch
                for (int i = 0; i < currentBatch; i++)
                {
                    int index = samplesEvaluated + i;

                    // Dims 0,1,2 | Dims 3,4,5 | Dims 6,7,8
                    Vector3 p1 = GetHaltonPoint(c1, index, 0);
                    Vector3 p2 = GetHaltonPoint(c2, index, 3);
                    Vector3 p3 = GetHaltonPoint(c3, index, 6);

                    Vector3 u = p2 - p1;
                    Vector3 v = p3 - p1;

                    // Scale-invariant collinearity check
                    if (u != Vector3.Zero && v != Vector3.Zero)
                    {
                        Vector3 normal = Vector3.Cross(Vector3.Normalize(u), Vector3.Normalize(v));
                        if (normal.LengthSquared() > 1e-6f)
                        {
                            normal = Vector3.Normalize(normal);
                            sumNormal += normal;
                            history.Add(normal);
                        }
                    }
                }

                samplesEvaluated += currentBatch;
                if (history.Count == 0) continue;

                // 2. Early Stopping Heuristic
                Vector3 currentMeanNormal = Vector3.Normalize(sumNormal);

                if (prevMeanNormal != Vector3.Zero)
                {
                    float dot = Math.Clamp(Vector3.Dot(currentMeanNormal, prevMeanNormal), -1f, 1f);
                    double angleDiff = Math.Acos(dot);

                    if (angleDiff <= stopThresholdRad)
                    {
                        break; // Convergence reached!
                    }
                }
                prevMeanNormal = currentMeanNormal;
            }

            if (history.Count == 0) return 0;

            // 3. Calculate Final Normal Uncertainty (U_f)
            Vector3 finalMeanNormal = Vector3.Normalize(sumNormal);
            double sumSqAngles = 0;

            foreach (var n in history)
            {
                float dot = Math.Clamp(Vector3.Dot(n, finalMeanNormal), -1f, 1f);
                double angle = Math.Acos(dot);
                sumSqAngles += angle * angle;
            }

            return Math.Sqrt(sumSqAngles / history.Count);
        }
    }
}