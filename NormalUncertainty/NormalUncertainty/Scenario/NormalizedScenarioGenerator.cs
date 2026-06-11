using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NormalUncertainty.Scenario
{
    public class GenerationConfig
    {
        public float MinSize { get; set; } = 0.01f;
        public float MaxSize { get; set; } = 10.0f;
        public float MinDistance { get; set; } = 0.01f;
        public float MaxDistance { get; set; } = 50.0f;
    }

    public class NormalizedScenarioGenerator
    {
        private readonly Random _r;
        private readonly GenerationConfig _config;

        public NormalizedScenarioGenerator(Random random, GenerationConfig config = null)
        {
            _r = random;
            _config = config ?? new GenerationConfig(); // Use defaults if not provided
        }

        public Scenario3D Generate()
        {
            // 1. Generate Cell A
            Vector3 aMin = Vector3.Zero;
            float aWidth = RandomFloat(_config.MinSize, _config.MaxSize);
            float aHeight = RandomFloat(_config.MinSize, _config.MaxSize);
            float aDepth = RandomFloat(_config.MinSize, _config.MaxSize);

            Vector3 aMax = new Vector3(aWidth, aHeight, aDepth);
            Vector3 centerA = aMax * 0.5f;

            // 2. Generate Cell B
            Vector3 centerB = centerA + GenerateRandomOffset();
            (Vector3 bMin, Vector3 bMax) = GenerateBounds(centerB);

            // 3. Generate Cell C
            Vector3 centerC = centerA + GenerateRandomOffset();
            (Vector3 cMin, Vector3 cMax) = GenerateBounds(centerC);

            var s = new Scenario3D(aMin, aMax, bMin, bMax, cMin, cMax);

            return s.Normalized();
        }

        private Vector3 GenerateRandomOffset()
        {
            // Rejection sampling for mathematically uniform 3D direction
            Vector3 dir;
            float sqMag;
            do
            {
                dir = new Vector3(
                    (float)_r.NextDouble() * 2f - 1f,
                    (float)_r.NextDouble() * 2f - 1f,
                    (float)_r.NextDouble() * 2f - 1f
                );
                sqMag = dir.LengthSquared();
            } while (sqMag > 1.0f || sqMag < 0.0001f);

            dir /= MathF.Sqrt(sqMag); // Normalize direction vector

            // Scale by random distance within our target bounds
            float dist = RandomFloat(_config.MinDistance, _config.MaxDistance);
            return dir * dist;
        }

        private (Vector3 Min, Vector3 Max) GenerateBounds(Vector3 center)
        {
            float sx = RandomFloat(_config.MinSize, _config.MaxSize);
            float sy = RandomFloat(_config.MinSize, _config.MaxSize);
            float sz = RandomFloat(_config.MinSize, _config.MaxSize);

            Vector3 halfSize = new Vector3(sx, sy, sz) * 0.5f;
            return (center - halfSize, center + halfSize);
        }

        private float RandomFloat(float min, float max)
        {
            return (float)_r.NextDouble() * (max - min) + min;
        }
    }
}
