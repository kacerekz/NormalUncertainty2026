using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NormalUncertainty.Experiments.Convergence._2D
{
    public class Scenario2D
    {
        private const float minSize = 0.5f;
        private const float maxSize = 2.0f;
        private const float minOffset = 0.5f;
        private const float maxOffset = 3.0f;

        public Vector2 boundsAMin;
        public Vector2 boundsAMax;
        public Vector2 boundsBMin;
        public Vector2 boundsBMax;

        public Scenario2D(Random r)
        {
            Vector2 offset = GenerateRandomOffset(r, minOffset, maxOffset);
            Vector2 centerA = Vector2.Zero;
            Vector2 centerB = centerA + offset;

            (boundsAMin, boundsAMax) = GenerateRandomBounds(r, centerA, minSize, maxSize);
            (boundsBMin, boundsBMax) = GenerateRandomBounds(r, centerB, minSize, maxSize);
        }

        private Scenario2D(Vector2 aMin, Vector2 aMax, Vector2 bMin, Vector2 bMax)
        {
            this.boundsAMin = aMin;
            this.boundsAMax = aMax;
            this.boundsBMin = bMin;
            this.boundsBMax = bMax;
        }

        public Scenario2D Clone()
        {
            return new Scenario2D(boundsAMin, boundsAMax, boundsBMin, boundsBMax);
        }

        private static (Vector2 Min, Vector2 Max) GenerateRandomBounds(Random r, Vector2 center, float minSize, float maxSize)
        {
            float x = (float)r.NextDouble() * (maxSize - minSize) + minSize;
            float y = (float)r.NextDouble() * (maxSize - minSize) + minSize;
            Vector2 halfSize = new Vector2(x * .5f, y * .5f);
            return (center - halfSize, center + halfSize);
        }

        private static Vector2 GenerateRandomOffset(Random r, float minOffset, float maxOffset)
        {
            float angle = (float)(r.NextDouble() * Math.PI * 2);
            Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            Vector2 offset = dir * (float)(minOffset + r.NextDouble() * (maxOffset - minOffset));
            return offset;
        }
    }
}
