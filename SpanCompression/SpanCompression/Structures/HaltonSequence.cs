using System;

namespace SpanCompression.Structures
{
    public static class HaltonSequence
    {
        private static float[][] _cache;
        private const int MaxCachedSamples = 50_000; // Enough for high precision

        // Thread safety
        static HaltonSequence()
        {
            int[] bases = { 2, 3, 5, 7, 11, 13, 17, 19, 23 };
            _cache = new float[9][];

            for (int d = 0; d < 9; d++)
            {
                _cache[d] = new float[MaxCachedSamples];
                int b = bases[d];
                for (int i = 0; i < MaxCachedSamples; i++)
                {
                    _cache[d][i] = GetHaltonSingle(i + 1, b);
                }
            }
        }

        public static float Get(int index, int dimension)
        {
            return _cache[dimension][index % MaxCachedSamples];
        }

        private static float GetHaltonSingle(int index, int @base)
        {
            float f = 1, r = 0;
            while (index > 0)
            {
                f /= @base;
                r += f * (index % @base);
                index /= @base;
            }
            return r;
        }
    }
}