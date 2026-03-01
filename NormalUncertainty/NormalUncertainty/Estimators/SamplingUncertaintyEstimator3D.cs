using NormalUncertainty.Samplers._3D;
using NormalUncertainty.Scenario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NormalUncertainty.Estimators
{
    public class SamplingUncertaintyEstimator3D : IUncertaintyEstimator3D
    {
        private readonly Func<Scenario3D, Sampler3D> _samplerFactory;
        private readonly Action<Sampler3D> _samplingStrategy;

        /// <summary>
        /// Initializes a new estimator with a specific sampler and an execution strategy.
        /// </summary>
        /// <param name="samplerFactory">How to create the sampler (e.g., s => new CornerSampler3D(s))</param>
        /// <param name="samplingStrategy">How to run the sampler (e.g., sampler => sampler.Sample(512) OR sampler => sampler.SampleUntilConvergence(100, 0.01f))</param>
        public SamplingUncertaintyEstimator3D(Func<Scenario3D, Sampler3D> samplerFactory, Action<Sampler3D> samplingStrategy)
        {
            _samplerFactory = samplerFactory;
            _samplingStrategy = samplingStrategy;
        }

        public float Estimate(Scenario3D scenario)
        {
            Sampler3D sampler = _samplerFactory(scenario);
            _samplingStrategy(sampler);
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
                sumSqAngles += Math.Pow(Math.Acos(dot), 2);
            }
            return (float)Math.Sqrt(sumSqAngles / history.Count);
        }
    }
}
