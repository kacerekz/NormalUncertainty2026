using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyLibrary
{
    public static class HaltonCache
    {
        private static float[][] _cache;
        private const int MaxCachedSamples = 200_000;
        private static bool _isInitialized = false;

        public static void Initialize()
        {
            if (_isInitialized) return;

            // 4 dimensions: Ax, Ay, Bx, By
            _cache = new float[4][];
            for (int d = 0; d < 4; d++) _cache[d] = new float[MaxCachedSamples];

            int[] bases = { 2, 3, 5, 7 };

            for (int d = 0; d < 4; d++)
            {
                int b = bases[d];
                for (int i = 0; i < MaxCachedSamples; i++)
                {
                    // Use the math from your existing Halton helper
                    _cache[d][i] = Halton.Get(i + 1, b);
                }
            }
            _isInitialized = true;
        }

        public static float GetValue(int index, int dimension)
        {
            if (!_isInitialized) Initialize();
            // Wrap around if we exceed cache size
            return _cache[dimension][index % MaxCachedSamples];
        }
    }
}
