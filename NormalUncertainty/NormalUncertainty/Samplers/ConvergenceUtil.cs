using MyLibrary;
using NormalUncertainty.Samplers._2D;
using NormalUncertainty.Samplers._3D;
using System.Numerics;

namespace NormalUncertainty.Samplers
{
    public static class ConvergenceUtil
    {
        /// <summary>
        /// Samples a 3D scenario in batches until the average normal stabilizes below the threshold.
        /// </summary>
        public static void SampleUntilConvergence(this Sampler3D sampler, int batchSize = 200, float maxChangeDegrees = 0.01f, int maxSamples = 100_000)
        {
            Vector3 runningSum = Vector3.Zero;
            int processedCount = 0;

            // Initial batch
            sampler.Sample(batchSize);

            // Prime the running sum
            for (int i = 0; i < sampler.NormalHistory.Count; i++)
            {
                runningSum += sampler.NormalHistory[i];
            }
            processedCount = sampler.NormalHistory.Count;

            Vector3 currentAverage = Vector3.Normalize(runningSum);

            while (sampler.NormalHistory.Count < maxSamples)
            {
                Vector3 lastAverage = currentAverage;

                int added = sampler.Sample(batchSize);
                if (added == 0) break; // Exhausted finite sampler

                // O(1) Trick: Only sum the NEWLY added vectors
                for (int i = processedCount; i < sampler.NormalHistory.Count; i++)
                {
                    runningSum += sampler.NormalHistory[i];
                }
                processedCount = sampler.NormalHistory.Count;

                if (runningSum == Vector3.Zero) continue;

                currentAverage = Vector3.Normalize(runningSum);

                if (lastAverage == Vector3.Zero) continue;

                float angleRad = MathUtil.UnsignedUnitVectorAngularDifferenceFast(lastAverage, currentAverage);
                if (MathUtil.ToDegrees(angleRad) <= maxChangeDegrees)
                    break;
            }
        }

        /// <summary>
        /// Samples a 2D scenario in batches until the average normal stabilizes below the threshold.
        /// </summary>
        public static void SampleUntilConvergence(this Sampler2D sampler, int batchSize = 200, float maxChangeDegrees = 0.01f, int maxSamples = 100_000)
        {
            Vector2 runningSum = Vector2.Zero;
            int processedCount = 0;

            sampler.Sample(batchSize);

            for (int i = 0; i < sampler.NormalHistory.Count; i++)
            {
                runningSum += sampler.NormalHistory[i];
            }
            processedCount = sampler.NormalHistory.Count;

            Vector2 currentAverage = Vector2.Normalize(runningSum);

            while (sampler.NormalHistory.Count < maxSamples)
            {
                Vector2 lastAverage = currentAverage;

                int added = sampler.Sample(batchSize);
                if (added == 0) break;

                for (int i = processedCount; i < sampler.NormalHistory.Count; i++)
                {
                    runningSum += sampler.NormalHistory[i];
                }
                processedCount = sampler.NormalHistory.Count;

                if (runningSum == Vector2.Zero) continue;

                currentAverage = Vector2.Normalize(runningSum);

                if (lastAverage == Vector2.Zero) continue;

                float angleRad = MathUtil.UnsignedUnitVectorAngularDifferenceFast(lastAverage, currentAverage);
                if (MathUtil.ToDegrees(angleRad) <= maxChangeDegrees)
                    break;
            }
        }
    }
}