using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using NormalUncertainty.Scenario;
using ScottPlot;

namespace NormalUncertainty.Experiments
{
    public class DatasetAnalyzer
    {
        public void RunAnalysis(string baseOutputDir, string plotsOutputDir)
        {
            Console.WriteLine($"Scanning {baseOutputDir} for evaluations.csv...");

            // 1. Feature Aggregation Lists
            List<double> aHeights = [], aDepths = [];
            List<double> bWidths = [], bHeights = [], bDepths = [];
            List<double> cWidths = [], cHeights = [], cDepths = [];

            // Spherical Coordinates for Offsets
            List<double> bDistances = [], bAzimuths = [], bElevations = [];
            List<double> cDistances = [], cAzimuths = [], cElevations = [];

            // 2. Batch Parsing
            var files = Directory.GetFiles(baseOutputDir, "evaluations.csv", SearchOption.AllDirectories);
            Console.WriteLine($"Found {files.Length} files to process.\n");

            foreach (var file in files)
            {
                var lines = File.ReadAllLines(file).Skip(1);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

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

                        Scenario3D s = new Scenario3D(aMin, aMax, bMin, bMax, cMin, cMax).Normalized();

                        // Feature Extraction: Dimensions
                        aHeights.Add(s.BoundsAMax.Y - s.BoundsAMin.Y);
                        aDepths.Add(s.BoundsAMax.Z - s.BoundsAMin.Z);

                        bWidths.Add(s.BoundsBMax.X - s.BoundsBMin.X);
                        bHeights.Add(s.BoundsBMax.Y - s.BoundsBMin.Y);
                        bDepths.Add(s.BoundsBMax.Z - s.BoundsBMin.Z);

                        cWidths.Add(s.BoundsCMax.X - s.BoundsCMin.X);
                        cHeights.Add(s.BoundsCMax.Y - s.BoundsCMin.Y);
                        cDepths.Add(s.BoundsCMax.Z - s.BoundsCMin.Z);

                        // Feature Extraction: Spherical Offsets
                        Vector3 centerA = (s.BoundsAMin + s.BoundsAMax) * 0.5f;
                        Vector3 centerB = (s.BoundsBMin + s.BoundsBMax) * 0.5f;
                        Vector3 centerC = (s.BoundsCMin + s.BoundsCMax) * 0.5f;

                        Vector3 offsetB = centerB - centerA;
                        Vector3 offsetC = centerC - centerA;

                        ExtractSpherical(offsetB, out double distB, out double azB, out double elB);
                        bDistances.Add(distB); bAzimuths.Add(azB); bElevations.Add(elB);

                        ExtractSpherical(offsetC, out double distC, out double azC, out double elC);
                        cDistances.Add(distC); cAzimuths.Add(azC); cElevations.Add(elC);
                    }
                    catch (Exception) { continue; }
                }
            }

            // 4. Console Statistics & Histograms
            Directory.CreateDirectory(plotsOutputDir);

            GenerateStatsAndHistogram(aHeights, "Cell_A_Height", plotsOutputDir);
            GenerateStatsAndHistogram(aDepths, "Cell_A_Depth", plotsOutputDir);

            GenerateStatsAndHistogram(bWidths, "Cell_B_Width", plotsOutputDir);
            GenerateStatsAndHistogram(bDistances, "Cell_B_Distance", plotsOutputDir);
            GenerateStatsAndHistogram(bAzimuths, "Cell_B_Azimuth", plotsOutputDir);
            GenerateStatsAndHistogram(bElevations, "Cell_B_Elevation", plotsOutputDir);

            GenerateStatsAndHistogram(cWidths, "Cell_C_Width", plotsOutputDir);
            GenerateStatsAndHistogram(cDistances, "Cell_C_Distance", plotsOutputDir);
            GenerateStatsAndHistogram(cAzimuths, "Cell_C_Azimuth", plotsOutputDir);
            GenerateStatsAndHistogram(cElevations, "Cell_C_Elevation", plotsOutputDir);

            Console.WriteLine("Analysis complete.");
            OpenFolder(plotsOutputDir);
        }

        private void ExtractSpherical(Vector3 offset, out double distance, out double azimuth, out double elevation)
        {
            distance = offset.Length();
            // Azimuth: Angle in X-Y plane (-180 to 180)
            azimuth = Math.Atan2(offset.Y, offset.X) * (180.0 / Math.PI);
            // Elevation: Angle above/below X-Y plane (-90 to 90)
            elevation = distance > 0 ? Math.Asin(offset.Z / distance) * (180.0 / Math.PI) : 0;
        }

        private float Parse(string value) => float.Parse(value, CultureInfo.InvariantCulture);

        private void GenerateStatsAndHistogram(List<double> values, string title, string outputDir)
        {
            if (values.Count == 0) return;

            // 1. Calculate and Print Statistics
            double min = values.Min();
            double max = values.Max();
            double avg = values.Average();
            double sumSq = values.Sum(v => Math.Pow(v - avg, 2));
            double stdDev = Math.Sqrt(sumSq / Math.Max(1, values.Count - 1));

            Console.WriteLine($"--- {title.Replace("_", " ")} ---");
            Console.WriteLine($"Min:    {min:F4}");
            Console.WriteLine($"Max:    {max:F4}");
            Console.WriteLine($"Avg:    {avg:F4}");
            Console.WriteLine($"StdDev: {stdDev:F4}\n");

            // 2. Fix flat data bounds
            if (Math.Abs(max - min) < 1e-6)
            {
                max = min + 1;
                min = min - 1;
            }

            // 3. ScottPlot 5.1 Generation
            Plot myPlot = new();
            var hist = ScottPlot.Statistics.Histogram.WithBinCount(count: 50, minValue: min, maxValue: max);
            hist.AddRange(values);

            var barPlot = myPlot.Add.Bars(hist.Bins, hist.Counts);
            barPlot.Color = Colors.Blue.WithAlpha(0.7);

            myPlot.Title(title.Replace("_", " "));
            myPlot.XLabel("Value");
            myPlot.YLabel("Frequency");
            myPlot.Add.Annotation($"Min: {min:F3}\nMax: {max:F3}\nAvg: {avg:F3}", Alignment.UpperRight);

            myPlot.SavePng(Path.Combine(outputDir, $"{title}.png"), 800, 600);
        }

        private void OpenFolder(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true, Verb = "open" });
            }
            catch (Exception ex) { Console.WriteLine($"Could not open directory: {ex.Message}"); }
        }
    }
}