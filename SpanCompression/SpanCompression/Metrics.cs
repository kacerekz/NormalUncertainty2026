using SpanCompression.Meshes;
using SpanCompression.Structures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SpanCompression
{
    public class Metrics
    {
        public static double FMPD(string filename1, string filename2)
        {
            var executable = "Metrics/FMPD/FMPD.exe";
            var arguments = $"\"{filename1}\" \"{filename2}\"";
            string? output = Utils.RunExecutable(executable, arguments);
            if (output == null)
                return double.NaN;
            //output = output.Trim();
            bool success = double.TryParse(output, out double value);
            return value;
        }

        public static double MSDM(string filename1, string filename2)
        {
            var executable = "Metrics/MSDM/MSDM.exe";
            var arguments = $"\"{filename1}\" \"{filename2}\"";
            string? output = Utils.RunExecutable(executable, arguments);
            if (output == null)
                return double.NaN;
            //output = output.Trim();
            bool success = double.TryParse(output, out double value);
            return value;
        }

        public static double MSDM2(string filename1, string filename2)
        {
            var executable = @"C:\Program Files (x86)\msdm2\example_msdm2.exe";
            var arguments = $"\"{filename1}\" \"{filename2}\"";
            string? output = Utils.RunExecutable(executable, arguments);
            if (output == null)
                return double.NaN;

            var split = output.Split('\n');
            foreach (var line in split)
                if (line.Trim().StartsWith("Calculated MSDM2")){
                    var value = line.Split(" ")[^1];
                    bool success = double.TryParse(value, out double result);
                    return result;
                }
            return double.NaN;
        }

        public static double DAME(string filename1, string filename2, bool p=false)
        {
            var executable = "Metrics/DAME/DameCL.exe";
            var arguments = $"\"{filename1}\" \"{filename2}\"";
            if (p) arguments += " -p";
            string? output = Utils.RunExecutable(executable, arguments);
            if (output == null)
                return double.NaN;
            var split = output.Split(" ");
            output = split[^1];
            output = output.Replace(",", ".");
            //output = output.Trim();
            bool success = double.TryParse(output, out double value);
            return value;
        }

        public static Dictionary<int, double> NUNC(CoarseMesh coarseMesh, ISampler sampler)
        {
            var cells = coarseMesh.Cells;
            var faces = coarseMesh.Faces;
            return NUNC(cells, faces, sampler);
        }

        public static Dictionary<int, double> NUNC(Cell[] cells, Face[] faces, ISampler sampler)
        {
            var samples = sampler.Sample(cells);
            return NormalUncertainty.Calculate(faces, samples);
        }

        public static double MSE(Mesh mesh, Mesh other)
        {
            if (mesh.Vertices.Length != other.Vertices.Length)
                return -1;

            var sum = 0.0;

            for (int i = 0; i < mesh.Vertices.Length; i++)
            {
                var diff = Vector3.DistanceSquared(mesh.Vertices[i], other.Vertices[i]);
                sum += diff;
            }

            return sum / mesh.Vertices.Length;
        }

        public static double MaxErr(Mesh mesh, Mesh other)
        {
            if (mesh.Vertices.Length != other.Vertices.Length)
                return -1;

            var max = 0.0;

            for (int i = 0; i < mesh.Vertices.Length; i++)
            {
                var diff = Vector3.Distance(mesh.Vertices[i], other.Vertices[i]);
                max = Math.Max(max, diff);
            }

            return max;
        }


    }
}
