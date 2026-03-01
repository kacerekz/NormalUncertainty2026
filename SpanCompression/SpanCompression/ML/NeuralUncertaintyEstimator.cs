using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SpanCompression.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SpanCompression.ML
{
    public class NeuralUncertaintyEstimator : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly string _inputName;

        public NeuralUncertaintyEstimator(string modelPath)
        {
            // Set session options for speed
            var options = new SessionOptions
            {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR
            };
            _session = new InferenceSession(modelPath, options);
            _inputName = _session.InputMetadata.Keys.First();
        }

        /// <summary>
        /// Predicts U_f for a batch of 3 scenarios: [Before, After0, After1]
        /// Runs sequentially to avoid shape mismatch errors with fixed-batch ONNX models.
        /// </summary>
        public (float before, float after0, float after1) PredictBatch(
            Cell b1, Cell b2, Cell b3,  // Scenario 1: Before Split
            Cell l1, Cell l2, Cell l3,  // Scenario 2: After Split Left (0)
            Cell r1, Cell r2, Cell r3)  // Scenario 3: After Split Right (1)
        {
            float before = Predict(b1, b2, b3);
            float after0 = Predict(l1, l2, l3);
            float after1 = Predict(r1, r2, r3);

            return (before, after0, after1);
        }

        /// <summary>
        /// Predicts U_f for a single scenario.
        /// </summary>
        public float Predict(Cell cellA, Cell cellB, Cell cellC)
        {
            // 1 row * 14 features = 14 floats
            float[] inputData = new float[14];

            // Fill features at offset 0
            FillFeatures(inputData, 0, cellA, cellB, cellC);

            // Create Tensor with shape [1, 14]
            var inputTensor = new DenseTensor<float>(inputData, new[] { 1, 14 });

            // Setup Inputs
            var inputs = NamedOnnxValue.CreateFromTensor(_inputName, inputTensor);

            // Run Inference
            using var results = _session.Run(new[] { inputs });

            // Extract Output (single float)
            return results.First().AsTensor<float>().GetValue(0);
        }

        /// <summary>
        /// Helper to normalize geometry and extract the 14 features expected by the Neural Net.
        /// Matches the Python training logic exactly.
        /// </summary>
        private void FillFeatures(float[] data, int offset, Cell A, Cell B, Cell C)
        {
            // Normalize relative to A
            // A.Min -> (0,0,0)
            // A.Size.X -> 1.0

            Vector3 shift = -A.Min;
            
            // Clamp to prevent scale exploding to Infinity when cells flatten
            float sizeX = Math.Max(A.SpanX.Size, 1e-7f);
            float scale = 1.0f / sizeX;

            // Feature 0-1: A Normalized Size (Height, Depth)
            data[offset + 0] = A.SpanY.Size * scale;
            data[offset + 1] = A.SpanZ.Size * scale;

            // Feature 2-4: B Min (Normalized)
            data[offset + 2] = (B.Min.X + shift.X) * scale;
            data[offset + 3] = (B.Min.Y + shift.Y) * scale;
            data[offset + 4] = (B.Min.Z + shift.Z) * scale;

            // Feature 5-7: B Max (Normalized)
            data[offset + 5] = (B.Max.X + shift.X) * scale;
            data[offset + 6] = (B.Max.Y + shift.Y) * scale;
            data[offset + 7] = (B.Max.Z + shift.Z) * scale;

            // Feature 8-10: C Min (Normalized)
            data[offset + 8] = (C.Min.X + shift.X) * scale;
            data[offset + 9] = (C.Min.Y + shift.Y) * scale;
            data[offset + 10] = (C.Min.Z + shift.Z) * scale;

            // Feature 11-13: C Max (Normalized)
            data[offset + 11] = (C.Max.X + shift.X) * scale;
            data[offset + 12] = (C.Max.Y + shift.Y) * scale;
            data[offset + 13] = (C.Max.Z + shift.Z) * scale;
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}