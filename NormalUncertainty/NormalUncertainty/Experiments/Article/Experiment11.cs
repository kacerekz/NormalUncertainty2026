using NormalUncertainty.Samplers._3D;
using NormalUncertainty.Scenario;
using ScottPlot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NormalUncertainty.Experiments.Article
{
    public class Experiment11
    {
        public void Run(string baseOutputDir, string plotsOutputDir, int targetScenarioCount = 2000)
        {
            int maxSamples = 100_000;
            int stepSize = 200;
            int totalSteps = maxSamples / stepSize;

            Console.WriteLine("--- Experiment 11: Monte Carlo Convergence ---");
            Console.WriteLine($"Target Scenarios: {targetScenarioCount:N0}");
            Console.WriteLine($"Max Samples: {maxSamples:N0} (Step: {stepSize})\n");

            // 1. Data Ingestion (Same random subset logic as Exp 10)
            var files = Directory.GetFiles(baseOutputDir, "evaluations.csv", SearchOption.AllDirectories);
            List<Scenario3D> testScenarios = [];
            Random fileRand = new Random();
            double selectionProbability = 0.001;

            Console.Write("Extracting random subset...");
            foreach (var file in files)
            {
                if (testScenarios.Count >= targetScenarioCount) break;
                foreach (var line in File.ReadLines(file).Skip(1))
                {
                    if (testScenarios.Count >= targetScenarioCount) break;
                    if (string.IsNullOrWhiteSpace(line) || fileRand.NextDouble() > selectionProbability) continue;

                    var p = line.Split([';', ','], StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length < 19) continue;

                    try
                    {
                        Vector3 aMin = new(Parse(p[0]), Parse(p[2]), Parse(p[4]));
                        Vector3 aMax = new(Parse(p[1]), Parse(p[3]), Parse(p[5]));
                        Vector3 bMin = new(Parse(p[6]), Parse(p[8]), Parse(p[10]));
                        Vector3 bMax = new(Parse(p[7]), Parse(p[9]), Parse(p[11]));
                        Vector3 cMin = new(Parse(p[12]), Parse(p[14]), Parse(p[16]));
                        Vector3 cMax = new(Parse(p[13]), Parse(p[15]), Parse(p[17]));

                        testScenarios.Add(new Scenario3D(aMin, aMax, bMin, bMax, cMin, cMax).Normalized());
                    }
                    catch (Exception) { /* Skip malformed */ }
                }
            }
            Console.WriteLine($" Extracted {testScenarios.Count:N0} scenarios.\n");

            // 2. Thread-Safe Aggregation Dictionary
            // Key: Sample count (e.g., 200, 400). Value: Bag of absolute changes in U_f (in degrees)
            var stepChanges = new ConcurrentDictionary<int, ConcurrentBag<double>>();
            for (int s = stepSize; s <= maxSamples; s += stepSize)
            {
                stepChanges[s] = new ConcurrentBag<double>();
            }

            // 3. Parallel Evaluation Loop
            int processed = 0;
            long startTicks = Stopwatch.GetTimestamp();
            Console.WriteLine("Evaluating convergence history...");

            Parallel.ForEach(testScenarios, (scenario) =>
            {
                var sampler = new BasicSampler3D(scenario, new Random(Guid.NewGuid().GetHashCode()));
                float previousUf = 0f;

                for (int currentStep = stepSize; currentStep <= maxSamples; currentStep += stepSize)
                {
                    // Sample the next batch
                    sampler.Sample(stepSize);

                    // Recalculate U_f over the entire accumulated history
                    float currentUf = CalculateUf(sampler.NormalHistory);

                    // Skip the very first step delta since previousUf was 0
                    if (currentStep > stepSize)
                    {
                        double deltaDeg = Math.Abs(currentUf - previousUf) * (180.0 / Math.PI);
                        stepChanges[currentStep].Add(deltaDeg);
                    }

                    previousUf = currentUf;
                }

                int currentCount = Interlocked.Increment(ref processed);
                if (currentCount % 100 == 0) Console.Write(".");
            });

            double elapsedSec = (Stopwatch.GetTimestamp() - startTicks) / (double)Stopwatch.Frequency;
            Console.WriteLine($"\nEvaluation completed in {elapsedSec:F2} seconds.");

            // 4. Generate the ScottPlot 5 Convergence Graphs
            Console.WriteLine("Generating plots...");
            Directory.CreateDirectory(plotsOutputDir);

            // Prepare data arrays for plotting
            int plotPoints = totalSteps - 1; // Skip the first step
            double[] xs = new double[plotPoints];

            // Arrays for Linear Plot
            double[] minYsLin = new double[plotPoints];
            double[] maxYsLin = new double[plotPoints];
            double[] avgYsLin = new double[plotPoints];

            // Arrays for Logarithmic Plot
            double[] minYsLog = new double[plotPoints];
            double[] maxYsLog = new double[plotPoints];
            double[] avgYsLog = new double[plotPoints];

            int index = 0;
            for (int s = stepSize * 2; s <= maxSamples; s += stepSize)
            {
                var deltas = stepChanges[s].ToList();
                if (deltas.Count == 0) continue;

                xs[index] = s;

                // 1. Populate Linear Data
                minYsLin[index] = deltas.Min();
                maxYsLin[index] = deltas.Max();
                avgYsLin[index] = deltas.Average();

                // 2. Populate Log10 Data (Clamped to prevent Log10(0) crashes)
                minYsLog[index] = Math.Log10(Math.Max(1e-5, deltas.Min()));
                maxYsLog[index] = Math.Log10(Math.Max(1e-5, deltas.Max()));
                avgYsLog[index] = Math.Log10(Math.Max(1e-5, deltas.Average()));

                index++;
            }

            // ==========================================
            // GRAPH 1: LINEAR SCALE
            // ==========================================
            Plot plotLin = new();
            plotLin.ScaleFactor = 7f;

            var fillLin = plotLin.Add.FillY(xs, minYsLin, maxYsLin);
            fillLin.FillColor = Colors.SteelBlue.WithAlpha(0.6);
            fillLin.LineColor = Colors.Transparent;

            var avgLineLin = plotLin.Add.ScatterLine(xs, avgYsLin);
            avgLineLin.Color = Colors.Navy;
            avgLineLin.LineWidth = 2;

            plotLin.Title("Monte Carlo Convergence (Absolute Change in U_f)");
            plotLin.XLabel("Number of Samples");
            plotLin.YLabel("Absolute Change (\u0394 Degrees)");

            string pathLin = Path.Combine(plotsOutputDir, "Convergence_Plot_Linear.png");
            plotLin.SavePng(pathLin, 4000, 2400);
            Console.WriteLine($"Linear Plot saved to {pathLin}");

            // ==========================================
            // GRAPH 2: LOGARITHMIC SCALE
            // ==========================================
            Plot plotLog = new();
            plotLog.ScaleFactor = 7f;

            var fillLog = plotLog.Add.FillY(xs, minYsLog, maxYsLog);
            fillLog.FillColor = Colors.SteelBlue.WithAlpha(0.6);
            fillLog.LineColor = Colors.Transparent;

            var avgLineLog = plotLog.Add.ScatterLine(xs, avgYsLog);
            avgLineLog.Color = Colors.Navy;
            avgLineLog.LineWidth = 2;

            plotLog.Title("Monte Carlo Convergence (Absolute Change in U_f)");
            plotLog.XLabel("Number of Samples");
            plotLog.YLabel("Absolute Change (\u0394 Degrees)");

            // Setup the Logarithmic Tick Generators
            ScottPlot.TickGenerators.LogMinorTickGenerator minorTickGen = new();
            ScottPlot.TickGenerators.NumericAutomatic tickGen = new();
            tickGen.MinorTickGenerator = minorTickGen;

            // Formatter: Convert the log-scaled Y coordinate back to a readable decimal
            static string LogTickLabelFormatter(double y) => $"{Math.Pow(10, y):0.#####}";

            tickGen.IntegerTicksOnly = true;
            tickGen.LabelFormatter = LogTickLabelFormatter;
            plotLog.Axes.Left.TickGenerator = tickGen;

            // Minor Grid Lines for logarithmic readability
            plotLog.Grid.MajorLineColor = Colors.Black.WithOpacity(.15);
            plotLog.Grid.MinorLineColor = Colors.Black.WithOpacity(.05);
            plotLog.Grid.MinorLineWidth = 1;

            string pathLog = Path.Combine(plotsOutputDir, "Convergence_Plot_Log.png");
            plotLog.SavePng(pathLog, 4000, 2400);
            Console.WriteLine($"Log Plot saved to {pathLog}");

            // Auto-open
            try { Process.Start(new ProcessStartInfo { FileName = plotsOutputDir, UseShellExecute = true, Verb = "open" }); }
            catch { }
        }

        private float CalculateUf(List<Vector3> history)
        {
            if (history.Count == 0) return 0;

            Vector3 sum = Vector3.Zero;
            foreach (var n in history) sum += n;
            Vector3 mean = Vector3.Normalize(sum);

            double sumSqAngles = 0;
            foreach (var n in history)
            {
                float dot = Math.Clamp(Vector3.Dot(n, mean), -1f, 1f);
                sumSqAngles += Math.Pow(Math.Acos(dot), 2);
            }
            return (float)Math.Sqrt(sumSqAngles / history.Count);
        }

        private float Parse(string value) => float.Parse(value, CultureInfo.InvariantCulture);
    }
}
