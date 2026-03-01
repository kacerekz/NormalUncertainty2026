using MyLibrary;
using NormalUncertainty.Scenario;
using System.Collections.Generic;
using System.Numerics;

namespace NormalUncertainty.Samplers._2D
{
    public class CachedHaltonSampler2D : Sampler2D
    {
        private readonly Scenario2D _scenario;
        private int _idx = 0;

        public CachedHaltonSampler2D(Scenario2D scenario)
        {
            _scenario = scenario;
        }

        public override int Sample(int count)
        {
            int samplesAdded = 0;
            while (samplesAdded < count)
            {
                float tAx = HaltonCache.Get(_idx, 0);
                float tAy = HaltonCache.Get(_idx, 1);
                float tBx = HaltonCache.Get(_idx, 2);
                float tBy = HaltonCache.Get(_idx, 3);

                _idx++;

                Vector2 pA = new Vector2(
                    _scenario.boundsAMin.X + tAx * (_scenario.boundsAMax.X - _scenario.boundsAMin.X),
                    _scenario.boundsAMin.Y + tAy * (_scenario.boundsAMax.Y - _scenario.boundsAMin.Y)
                );

                Vector2 pB = new Vector2(
                    _scenario.boundsBMin.X + tBx * (_scenario.boundsBMax.X - _scenario.boundsBMin.X),
                    _scenario.boundsBMin.Y + tBy * (_scenario.boundsBMax.Y - _scenario.boundsBMin.Y)
                );

                Vector2 line = pB - pA;
                Vector2 normal = new(-line.Y, line.X);

                if (normal.LengthSquared() > 0.00001f)
                {
                    NormalHistory.Add(Vector2.Normalize(normal));
                    samplesAdded++;
                }
            }
            return samplesAdded;
        }
    }
}