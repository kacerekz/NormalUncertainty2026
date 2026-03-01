using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpanCompression._3rdParty
{
    public class Edgebreaker
    {
        public static EdgebreakerOutput? Run(string filename, float precision)
        {
            var executable = "3rdParty/Edgebreaker/compress.exe";
            var arguments = $"\"{filename}\" {precision} -s";
            string? output = Utils.RunExecutable(executable, arguments);
            if (output == null)
                return null;
            return new EdgebreakerOutput(output);
        }
    }

    public class EdgebreakerOutput
    {
        public string OutputPath { get; set; }
        public int GeometryBits { get; set; }
        public int ConnectivityBits { get; set; }
        public int PointCount { get; set; }

        public EdgebreakerOutput(string output)
        {
            var split = output.Split("\n");

            int connectivityBits = int.Parse(split[^4]);
            int geometryBits = int.Parse(split[^3]);
            int pointCount = int.Parse(split[^2]);

            OutputPath = "3rdParty/Edgebreaker/simulation.obj";
            GeometryBits = geometryBits * 8;
            ConnectivityBits = connectivityBits * 8;
            PointCount = pointCount;
        }

        public override string ToString()
        {
            return
                "Edgebreaker output:\n" +
                $"output path = {OutputPath}\n" +
                $"point count = {PointCount}\n" +
                $"bits geom.  = {GeometryBits}\n" +
                $"bits conn.  = {ConnectivityBits}\n" +
                $"bits total  = {ConnectivityBits + GeometryBits}\n";
        }
    }
}
