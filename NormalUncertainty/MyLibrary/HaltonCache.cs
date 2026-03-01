using System;
using System.Collections.Generic;

namespace MyLibrary
{
    public static class HaltonCache
    {
        private static float[][] _cache;
        private static int _sampleCount;
        public static bool IsInitialized { get; private set; } = false;

        public static void Initialize(int dimensions, int samples = 50_000)
        {
            _sampleCount = samples;
            _cache = new float[dimensions][];

            int[] bases = GetPrimes(dimensions);

            for (int d = 0; d < dimensions; d++)
            {
                _cache[d] = new float[_sampleCount];
                int b = bases[d];

                for (int i = 0; i < _sampleCount; i++)
                {
                    _cache[d][i] = Halton.Get(i + 1, b);
                }
            }

            IsInitialized = true;
        }

        public static float Get(int index, int dimension)
        {
            if (!IsInitialized /*|| !InRange(index)*/)
                return float.NaN;

            // HACK: Call the manual calculation for that index instead?
            // Or make cache dynamically sized?
            return _cache[dimension][index % _sampleCount];
        }

        public static bool InRange(int index)
        {
            return index >= 0 && index < _sampleCount;
        }

        private static int[] GetPrimes(int n)
        {
            List<int> primes = new List<int>();
            int num = 2;
            while (primes.Count < n)
            {
                if (IsPrime(num)) primes.Add(num);
                num++;
            }
            return primes.ToArray();
        }

        private static bool IsPrime(int number)
        {
            if (number < 2) return false;
            if (number == 2) return true;
            if (number % 2 == 0) return false;
            for (int i = 3; i <= Math.Sqrt(number); i += 2)
            {
                if (number % i == 0) return false;
            }
            return true;
        }
    }
}