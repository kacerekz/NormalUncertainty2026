using MyLibrary;
using System.Collections.Generic;
using System.Numerics;

namespace NormalUncertainty.Experiments.Convergence._2D
{
    public class CachedHaltonSampler2D : ISamplingStrategy2D
    {
        private readonly Scenario2D _scenario;
        private int _sampleIndex = 0;

        public List<Vector2> NormalHistory { get; } = new();

        public CachedHaltonSampler2D(Scenario2D scenario)
        {
            _scenario = scenario;
            HaltonCache.Initialize(); // Ensure cache is ready
        }

        public int Sample(int count)
        {
            int samplesAdded = 0;
            while (samplesAdded < count)
            {
                // Simple array lookups (Dimensions 0,1,2,3)
                float tAx = HaltonCache.GetValue(_sampleIndex, 0);
                float tAy = HaltonCache.GetValue(_sampleIndex, 1);
                float tBx = HaltonCache.GetValue(_sampleIndex, 2);
                float tBy = HaltonCache.GetValue(_sampleIndex, 3);

                _sampleIndex++;

                Vector2 pA = new Vector2(
                    _scenario.boundsAMin.X + tAx * (_scenario.boundsAMax.X - _scenario.boundsAMin.X),
                    _scenario.boundsAMin.Y + tAy * (_scenario.boundsAMax.Y - _scenario.boundsAMin.Y)
                );

                Vector2 pB = new Vector2(
                    _scenario.boundsBMin.X + tBx * (_scenario.boundsBMax.X - _scenario.boundsBMin.X),
                    _scenario.boundsBMin.Y + tBy * (_scenario.boundsBMax.Y - _scenario.boundsBMin.Y)
                );

                Vector2 line = pB - pA;
                Vector2 normal = new Vector2(-line.Y, line.X);

                if (normal.LengthSquared() > 0.00001f)
                {
                    NormalHistory.Add(Vector2.Normalize(normal));
                    samplesAdded++;
                }
            }
            return samplesAdded;
        }

        public Vector2 GetAverageNormal()
        {
            Vector2 average = Vector2.Zero;
            foreach (var n in NormalHistory) average += n;
            return Vector2.Normalize(average);
        }
    }
}