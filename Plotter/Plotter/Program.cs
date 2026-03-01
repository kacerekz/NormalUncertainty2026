using ScottPlot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Plotter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            string name = "bunny";
            int vertices = 1002;
            string fileBase = @"C:\Users\adria\Desktop\NUNC2 data\results\corner\-28-02-2026-(11-14-47)\edgebreaker.metrics.txt";
            string fileHalton = @"C:\Users\adria\Desktop\NUNC2 data\results\halton\-28-02-2026-(11-57-48)\nunc.predictor1.metrics.txt";
            string fileCorner = @"C:\Users\adria\Desktop\NUNC2 data\results\corner\-28-02-2026-(11-14-47)\nunc.predictor1.metrics.txt";
            string fileNeural = @"C:\Users\adria\Desktop\NUNC2 data\results\neural\-28-02-2026-(13-25-49)\nunc.predictor1.metrics.txt";

            //string name = "lion";
            //int vertices = 2213;
            //string fileBase = @"C:\Users\adria\Desktop\NUNC2 data\results\corner\-28-02-2026-(11-20-55)\edgebreaker.metrics.txt";
            //string fileHalton = @"C:\Users\adria\Desktop\NUNC2 data\results\halton\-28-02-2026-(12-07-27)\nunc.predictor1.metrics.txt";
            //string fileCorner = @"C:\Users\adria\Desktop\NUNC2 data\results\corner\-28-02-2026-(11-20-55)\nunc.predictor1.metrics.txt";
            //string fileNeural = @"C:\Users\adria\Desktop\NUNC2 data\results\neural\-28-02-2026-(13-31-34)\nunc.predictor1.metrics.txt";

            //string name = "fandisk";
            //int vertices = 6475;
            //string fileBase = @"C:\Users\adria\Desktop\NUNC2 data\results\corner\-28-02-2026-(11-22-41)\edgebreaker.metrics.txt";
            //string fileHalton = @"C:\Users\adria\Desktop\NUNC2 data\results\halton\-28-02-2026-(12-09-45)\nunc.predictor1.metrics.txt";
            //string fileCorner = @"C:\Users\adria\Desktop\NUNC2 data\results\corner\-28-02-2026-(11-22-41)\nunc.predictor1.metrics.txt";
            //string fileNeural = @"C:\Users\adria\Desktop\NUNC2 data\results\neural\-28-02-2026-(13-33-19)\nunc.predictor1.metrics.txt";

            //string name = "armadillo";
            //int vertices = 5002;
            //string fileBase = @"C:\Users\adria\Desktop\NUNC2 data\results\corner\-28-02-2026-(11-16-21)\edgebreaker.metrics.txt";
            //string fileHalton = @"C:\Users\adria\Desktop\NUNC2 data\results\halton\-28-02-2026-(11-59-34)\nunc.predictor1.metrics.txt";
            //string fileCorner = @"C:\Users\adria\Desktop\NUNC2 data\results\corner\-28-02-2026-(11-16-21)\nunc.predictor1.metrics.txt";
            //string fileNeural = @"C:\Users\adria\Desktop\NUNC2 data\results\neural\-28-02-2026-(13-27-20)\nunc.predictor1.metrics.txt";

            //string name = "planck";
            //int vertices = 25445;
            //string fileBase = @"C:\Users\adria\Desktop\NUNC2 data\results\corner\-28-02-2026-(11-26-15)\edgebreaker.metrics.txt";
            //string fileHalton = @"C:\Users\adria\Desktop\NUNC2 data\results\halton\-28-02-2026-(12-13-58)\nunc.predictor1.metrics.txt";
            //string fileCorner = @"C:\Users\adria\Desktop\NUNC2 data\results\corner\-28-02-2026-(11-26-15)\nunc.predictor1.metrics.txt";
            //string fileNeural = @"C:\Users\adria\Desktop\NUNC2 data\results\neural\-28-02-2026-(13-36-32)\nunc.predictor1.metrics.txt";

            // Load metrics
            var metricsBase = new MetricsFile(fileBase);
            var metricsHalton = new MetricsFile(fileHalton);
            var metricsCorner = new MetricsFile(fileCorner);
            var metricsNeural = new MetricsFile(fileNeural);

            // Print baseline comparisons
            Console.WriteLine($"--- Bitrate & Comparisons for {name} ---");
            var bitrate = metricsBase.GetColumn(0).Last() / (float)vertices;
            Console.WriteLine("Baseline Bitrate: " + bitrate);

            PrintMetricComparisons("Halton", metricsBase, metricsHalton);
            PrintMetricComparisons("Corner", metricsBase, metricsCorner);
            PrintMetricComparisons("Neural", metricsBase, metricsNeural);

            // Prepare scaled bits arrays (X-axis)
            double[] bitsEdge = GetScaledBits(metricsBase, vertices);
            double[] bitsHalton = GetScaledBits(metricsHalton, vertices);
            double[] bitsCorner = GetScaledBits(metricsCorner, vertices);
            double[] bitsNeural = GetScaledBits(metricsNeural, vertices);

            string[] metricNames = { "DAME", "FMPD", "MSDM", "MSDM2" };

            foreach (string metricName in metricNames)
            {
                // Extract metric data (Y-axis)
                double[] metricEdge = metricsBase.GetColumn(metricName);
                double[] metricHalton = metricsHalton.GetColumn(metricName);
                double[] metricCorner = metricsCorner.GetColumn(metricName);
                double[] metricNeural = metricsNeural.GetColumn(metricName);

                var plt = new Plot();

                // Add curves
                var s1 = plt.Add.Scatter(bitsEdge, metricEdge);
                s1.LegendText = "Edgebreaker + WPP";

                var s2 = plt.Add.Scatter(bitsHalton, metricHalton);
                s2.LegendText = "Our method (Halton)";

                var s3 = plt.Add.Scatter(bitsCorner, metricCorner);
                s3.LegendText = "Our method (Corner)";

                var s4 = plt.Add.Scatter(bitsNeural, metricNeural);
                s4.LegendText = "Our method (Neural)";

                // -- Conditional Grid Formatting --

                // X-Axis: "Bitrate" label only on the bottom-middle graph (fandisk, MSDM2)
                if (name == "fandisk" && metricName == "MSDM2")
                    plt.Axes.Bottom.Label.Text = "Bitrate";
                else
                    plt.Axes.Bottom.Label.Text = " ";

                // Y-Axis: Metric name only on the leftmost graphs (armadillo)
                if (name == "armadillo")
                    plt.Axes.Left.Label.Text = metricName;
                else
                    plt.Axes.Left.Label.Text = string.Empty;

                // Legend: Visible only on the top-right graph (planck, DAME)
                plt.Legend.Alignment = Alignment.UpperRight;
                if (name == "planck" && metricName == "DAME")
                    plt.Legend.IsVisible = true;
                else
                    plt.Legend.IsVisible = false;

                // -- High-Resolution Export --
                // Scale factor increased to 8.5 to maintain visual weight at 3750x2500 resolution.
                plt.ScaleFactor = 8.5f;
                plt.SavePng($"{name}_{metricName}.png", 3750, 2500);
            }
        }

        private static double[] GetScaledBits(MetricsFile file, int vertices)
        {
            double[] bits = file.GetColumn("bits");
            for (int i = 0; i < bits.Length; i++)
            {
                bits[i] /= (double)vertices;
            }
            return bits;
        }

        private static void PrintMetricComparisons(string variantName, MetricsFile baseline, MetricsFile variant)
        {
            var percentChanges = CompareLastRowPercentage(baseline, variant);
            Console.WriteLine($"\nVersus {variantName}:");
            foreach (var kvp in percentChanges)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value:+0.00;-0.00;0.00}%");
            }
        }

        public static Dictionary<string, double> CompareLastRowPercentage(MetricsFile file1, MetricsFile file2)
        {
            var result = new Dictionary<string, double>();

            if (!file1.ColumnNames.SequenceEqual(file2.ColumnNames))
                throw new Exception("Metric columns don't match between files.");

            for (int i = 0; i < file1.ColumnNames.Count; i++)
            {
                string name = file1.ColumnNames[i];
                var col1 = file1.DataColumns[i];
                var col2 = file2.DataColumns[i];

                if (col1.Count == 0 || col2.Count == 0)
                {
                    result[name] = double.NaN;
                    continue;
                }

                double last1 = col1[^1];
                double last2 = col2[^1];

                if (double.IsNaN(last1) || last1 == 0.0)
                {
                    result[name] = double.NaN;
                }
                else
                {
                    double percentDifference = 100.0 * (last2 - last1) / last1;
                    result[name] = percentDifference;
                }
            }

            return result;
        }
    }
}