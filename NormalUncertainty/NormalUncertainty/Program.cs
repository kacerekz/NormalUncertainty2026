
using MyLibrary;
using NormalUncertainty.Experiments;
using NormalUncertainty.Experiments.Article;
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

            //var generator = new AdaptiveHaltonDatasetGenerator3D();
            //generator.Generate(1_000_000, "dataset.csv");

            //var processedDir = @"C:\Users\adria\Desktop\NormalUncertainty2025-main\SpanCompression\SpanCompression\bin\Release\net8.0\output";
            //var processedDir = @"C:\Users\adria\Desktop\NormalUncertainty2025-main\SpanCompression\SpanCompression\bin\Release\net8.0\output\-21-02-2026-(20-40-57)";
            //var analyzer = new DatasetAnalyzer();
            //analyzer.RunAnalysis(processedDir, "histograms fandisk");

            //var experiment = new Experiment09();
            //experiment.Run();

            //var resultsDir = @"C:\Users\adria\Desktop\NormalUhttps://gemini.google.com/app/ef837b0c18aab872ncertainty2025-main\SpanCompression\SpanCompression\bin\Release\net8.0\output";
            //var experiment = new Experiment10();
            //experiment.Run(resultsDir, 100_000, 100_000);

            var resultsDir = @"C:\Users\adria\Desktop\NormalUncertainty2025-main\SpanCompression\SpanCompression\bin\Release\net8.0\output";
            var experiment = new Experiment11();
            experiment.Run(resultsDir, "11", 2_000);

            //var resultsDir = @"C:\Users\adria\Desktop\NormalUncertainty2025-main\SpanCompression\SpanCompression\bin\Release\net8.0\output";
            //var experiment = new Experiment12();
            //experiment.Run(resultsDir, 20_000, 200, 0.05f);

            //var resultsDir = @"C:\Users\adria\Desktop\NormalUncertainty2025-main\SpanCompression\SpanCompression\bin\Release\net8.0\output";
            //var modelPath = @"models\v7\uncertainty_model_3d.onnx";
            //var experiment = new Experiment13();
            //experiment.Run(resultsDir, modelPath, 20_000, 200, 0.05f);
        }
    }
}
