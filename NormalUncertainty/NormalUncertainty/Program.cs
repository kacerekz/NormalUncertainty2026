
using MyLibrary;
using NormalUncertainty.Experiments.ML;
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

            var generator = new AdaptiveHaltonDatasetGenerator3D();
            generator.Generate(1_000_000, "dataset_3d_ADAH_1M.csv");

            //var experiment = new Experiment09();
            //experiment.Run();
        }
    }
}
