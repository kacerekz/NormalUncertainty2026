using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NormalUncertainty.Scenario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace NormalUncertainty.Estimators
{
    public class NeuralUncertaintyEstimator3D : IUncertaintyEstimator3D, IDisposable
    {
        private readonly InferenceSession _session;

        public NeuralUncertaintyEstimator3D(string modelPath)
        {
            _session = new InferenceSession(modelPath);
        }

        public float Estimate(Scenario3D s)
        {
            float[] inputData = s.Normalized().GetNetworkInput();
            var inputTensor = new DenseTensor<float>(inputData, new[] { 1, 14 });

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input", inputTensor)
            };

            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = _session.Run(inputs);
            return results.First().AsTensor<float>().First();
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}