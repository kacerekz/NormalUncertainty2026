using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace MyLibrary
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

        //public Vector3 FaceNormal(int f)
        //{
        //    var face = Faces[f];

        //    var v1 = Vertices[face.V1];
        //    var v2 = Vertices[face.V2];
        //    var v3 = Vertices[face.V3];

        //    var normal = Vector3.Cross(v3 - v1, v2 - v1);
        //    normal = Vector3.Normalize(normal);
        //    return normal;
        //}

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
                        face.V1 + vertexOffset,
                        face.V2 + vertexOffset,
                        face.V3 + vertexOffset
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
                    sw.WriteLine($"f {f.V1 + 1} {f.V2 + 1} {f.V3 + 1}");
                }
            }
        }
    }

}
