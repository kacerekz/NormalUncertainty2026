using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Plotter
{
    public class MetricsFile
    {
        public string FilePath { get; private set; }
        public List<string> ColumnNames { get; private set; } = new List<string>();
        public List<List<double>> DataColumns { get; private set; } = new List<List<double>>();

        public MetricsFile(string filepath)
        {
            FilePath = filepath;
            LoadFile(filepath);
        }

        private void LoadFile(string filepath)
        {
            var lines = File.ReadAllLines(filepath);

            if (lines.Length < 2)
                throw new Exception("File format is incorrect or file too short.");

            // First line is the file path, ignore
            // Second line is header with column names
            ColumnNames = lines[1].Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

            // Initialize lists for each column
            DataColumns = new List<List<double>>();
            for (int i = 0; i < ColumnNames.Count; i++)
                DataColumns.Add(new List<double>());

            // Parse data lines (from line index 2 onwards)
            for (int i = 2; i < lines.Length; i++)
            {
                var parts = lines[i].Split(';', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < ColumnNames.Count)
                    continue; // skip malformed lines or incomplete

                for (int c = 0; c < ColumnNames.Count; c++)
                {
                    if (double.TryParse(parts[c], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val))
                    {
                        DataColumns[c].Add(val);
                    }
                    else
                    {
                        // Could not parse, add NaN or 0 or throw exception
                        DataColumns[c].Add(double.NaN);
                    }
                }
            }
        }

        // Get column by index
        public double[] GetColumn(int index)
        {
            if (index < 0 || index >= DataColumns.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return DataColumns[index].ToArray();
        }

        // Get column by name
        public double[] GetColumn(string name)
        {
            int idx = ColumnNames.IndexOf(name);
            if (idx == -1)
                throw new ArgumentException($"Column '{name}' not found.");
            return GetColumn(idx);
        }
    }
}