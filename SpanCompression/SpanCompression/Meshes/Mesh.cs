using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SpanCompression.Structures;

namespace SpanCompression.Meshes
{
    public class Mesh
    {
        public (int, int)[] Lines { get; set; } = [];
        public Vector3[] Vertices { get; set; } = [];
        public Vector3[] Colors { get; set; } = [];
        public Vector3[] Normals { get; set; } = [];
        public Face[] Faces { get; set; } = [];

        public bool HasColors => Colors.Length == Vertices.Length && Vertices.Length > 0;
        public bool HasNormals => Normals.Length == Vertices.Length && Vertices.Length > 0;

        public static Mesh ConcatenateMeshes(Mesh[] meshes)
        {
            List<(int, int)> combinedLines = [];
            List<Vector3> combinedVertices = [];
            List<Vector3> combinedNormals = [];
            List<Vector3> combinedColors = [];
            List<Face> combinedFaces = [];

            int vertexOffset = 0;

            foreach (var mesh in meshes)
            {
                combinedVertices.AddRange(mesh.Vertices);
                combinedNormals.AddRange(mesh.Normals);
                combinedColors.AddRange(mesh.Colors);

                foreach (var face in mesh.Faces)
                {
                    combinedFaces.Add(new Face(
                        face.i1 + vertexOffset,
                        face.i2 + vertexOffset,
                        face.i3 + vertexOffset
                    ));
                }

                foreach (var line in mesh.Lines)
                {
                    combinedLines.Add((
                        line.Item1 + vertexOffset,
                        line.Item2 + vertexOffset
                    ));
                }

                vertexOffset += mesh.Vertices.Length;
            }

            return new Mesh
            {
                Vertices = [.. combinedVertices],
                Normals = [.. combinedNormals],
                Colors = [.. combinedColors],
                Faces = [.. combinedFaces],
                Lines = [.. combinedLines]
            };
        }

        public void Write(string path)
        {
            using var sw = new StreamWriter(path);

            if (!HasColors)
                foreach (var v in Vertices)
                {
                    sw.WriteLine($"v {v.X} {v.Y} {v.Z}");
                }

            else
                for (int i = 0; i < Vertices.Length; i++)
                {
                    var v = Vertices[i];
                    var c = Colors[i];
                    sw.WriteLine($"v {v.X} {v.Y} {v.Z} {c.X} {c.Y} {c.Z}");
                }

            if (HasNormals)
                foreach (var vn in Normals)
                {
                    sw.WriteLine($"vn {vn.X} {vn.Y} {vn.Z}");
                }

            if (Lines.Length > 0)
            {
                for (int i = 0; i < Lines.Length; i++)
                {
                    var l = Lines[i];
                    sw.WriteLine($"l {l.Item1 + 1} {l.Item2 + 1}");
                }
            }
            else
            {
                for (int i = 0; i < Faces.Length; i++)
                {
                    var f = Faces[i];
                    sw.WriteLine($"f {f.i1 + 1} {f.i2 + 1} {f.i3 + 1}");
                }
            }
        }

        public void ColorByValues(double[] values)
        {
            double max = values.Max();

            Colors = new Vector3[Vertices.Length];
            
            for (int i = 0; i < Vertices.Length; i++)
            {
                double t = Math.Clamp(values[i] / max, 0.0, 1.0);

                float r = 1f;
                float g = (float)(1.0 - t);
                float b = g;

                Colors[i] = new Vector3(r, g, b);
            }
        }

        public void ColorByInverseChannelIntensity(double[][] values)
        {
            int count = values.Length;
            Vector3[] colors = new Vector3[count];

            // Step 1: find max per channel
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            double maxZ = double.MinValue;

            foreach (var v in values)
            {
                maxX = Math.Max(maxX, v[0]);
                maxY = Math.Max(maxY, v[1]);
                maxZ = Math.Max(maxZ, v[2]);
            }

            // Step 2: generate inverted intensity-based color
            for (int i = 0; i < count; i++)
            {
                double rRatio = maxX > 0 ? values[i][0] / maxX : 0;
                double gRatio = maxY > 0 ? values[i][1] / maxY : 0;
                double bRatio = maxZ > 0 ? values[i][2] / maxZ : 0;

                // Invert and rigidly clamp between 0.0 and 1.0
                double r = Math.Clamp(1.0 - rRatio, 0.0, 1.0);
                double g = Math.Clamp(1.0 - gRatio, 0.0, 1.0);
                double b = Math.Clamp(1.0 - bRatio, 0.0, 1.0);

                colors[i] = new Vector3((float)r, (float)g, (float)b);
            }

            Colors = colors;
        }
    }
}
