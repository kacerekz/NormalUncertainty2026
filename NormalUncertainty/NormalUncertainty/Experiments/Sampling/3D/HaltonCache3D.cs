using MyLibrary;
using NormalUncertainty.Experiments.Convergence._2D; // For Halton helper

namespace NormalUncertainty.Experiments.Convergence._3D
{
    public static class HaltonCache3D
    {
        private static float[][] _cache;
        private const int MaxCachedSamples = 200_000;
        private static bool _isInitialized = false;

        public static void Initialize()
        {
            if (_isInitialized) return;

            // 9 Dimensions: 
            // A(x,y,z) -> Bases 2, 3, 5
            // B(x,y,z) -> Bases 7, 11, 13
            // C(x,y,z) -> Bases 17, 19, 23
            int[] bases = { 2, 3, 5, 7, 11, 13, 17, 19, 23 };

            _cache = new float[9][];
            for (int d = 0; d < 9; d++)
            {
                _cache[d] = new float[MaxCachedSamples];
                int b = bases[d];
                for (int i = 0; i < MaxCachedSamples; i++)
                {
                    _cache[d][i] = Halton.Get(i + 1, b);
                }
            }
            _isInitialized = true;
        }

        public static float Get(int index, int dimension)
        {
            if (!_isInitialized) Initialize();
            return _cache[dimension][index % MaxCachedSamples];
        }
    }
}