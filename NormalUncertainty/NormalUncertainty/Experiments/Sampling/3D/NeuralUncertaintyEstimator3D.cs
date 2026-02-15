using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NormalUncertainty.Experiments.Convergence._3D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace NormalUncertainty.Experiments.ML
{
    public class NeuralUncertaintyEstimator3D : IDisposable
    {
        private readonly InferenceSession _session;

        public NeuralUncertaintyEstimator3D(string modelPath)
        {
            _session = new InferenceSession(modelPath);
        }

        public float PredictUf(Scenario3D s)
        {
            // 1. Normalize strictly to match the Python training data
            Vector3 shift = -s.BoundsAMin;
            float scale = 1.0f / (s.BoundsAMax.X - s.BoundsAMin.X);

            Vector3 aSize = (s.BoundsAMax - s.BoundsAMin) * scale;
            Vector3 bMin = (s.BoundsBMin + shift) * scale;
            Vector3 bMax = (s.BoundsBMax + shift) * scale;
            Vector3 cMin = (s.BoundsCMin + shift) * scale;
            Vector3 cMax = (s.BoundsCMax + shift) * scale;

            // 2. Map the 14 Features
            float[] inputData = new float[]
            {
                aSize.Y, aSize.Z,
                bMin.X, bMin.Y, bMin.Z, bMax.X, bMax.Y, bMax.Z,
                cMin.X, cMin.Y, cMin.Z, cMax.X, cMax.Y, cMax.Z
            };

            // 3. Create Tensor [BatchSize=1, Features=14]
            var inputTensor = new DenseTensor<float>(inputData, new[] { 1, 14 });
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input", inputTensor)
            };

            // 4. Run Inference
            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = _session.Run(inputs);

            // Returns U_f in Radians
            return results.First().AsTensor<float>().First();
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}