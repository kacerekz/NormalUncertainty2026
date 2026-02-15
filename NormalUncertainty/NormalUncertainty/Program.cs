
using MyLibrary;
using NormalUncertainty.Experiments;
using NormalUncertainty.Experiments.Convergence;
using NormalUncertainty.Experiments.Convergence._2D;
using NormalUncertainty.Experiments.Convergence._3D;
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

            var generator = new RandomUncertaintyDatasetGenerator3D();
            generator.GenerateParallel(1_000_000, "dataset_3d_1M.csv");

            //var experiment = new Experiment09();
            //experiment.Run();
        }
    }
}
