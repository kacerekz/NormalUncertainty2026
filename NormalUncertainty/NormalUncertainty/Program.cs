
using MyLibrary;
using NormalUncertainty.Experiments;
using NormalUncertainty.Experiments.Convergence;
using OpenTkRenderer;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using System.Reflection;

namespace NormalUncertainty
{
    internal class Program
    {
        private static readonly Random _random = new Random();

        static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            // Make 2 example quads
            // Generate points within them randomly (use same method as original project) (consult AI for alternatives)
            // Connect point pair into line, calculate its normal
            // Update average normal. Update average deviation from average normal?
            // Plot the CHANGE in average deviation as new samples are added.
            // Observe it decreasing?

            var experiment = new Convergence2dExperiment2() { Width = 800 , Height = 600 };
            experiment.Run("Random Sampling Demo");

        }
    }
}
