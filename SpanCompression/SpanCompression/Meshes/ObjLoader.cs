using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SpanCompression.Structures;

namespace SpanCompression.Meshes
{
    public class ObjLoader
    {
        public static Mesh Load(string path)
        {
            using var sr = new StreamReader(path);

            var vertices = new List<Vector3>();
            var colors = new List<Vector3>();
            var normals = new List<Vector3>();
            var faces = new List<Face>();

            string? line;

            while ((line = sr.ReadLine()) != null)
            {
                var split = line.Split(' ');

                switch (split[0])
                {
                    case "v":
                        var v = new Vector3
                        {
                            X = float.Parse(split[1]),
                            Y = float.Parse(split[2]),
                            Z = float.Parse(split[3])
                        };
                        vertices.Add(v);

                        if (split.Length > 4)
                        {
                            var c = new Vector3
                            {
                                X = float.Parse(split[4]),
                                Y = float.Parse(split[5]),
                                Z = float.Parse(split[6])
                            };
                            colors.Add(c);
                        }
                        break;

                    case "vn":
                        var vn = new Vector3
                        {
                            X = float.Parse(split[1]),
                            Y = float.Parse(split[2]),
                            Z = float.Parse(split[3])
                        };
                        normals.Add(vn);
                        break;

                    case "f":
                        int faceIndex = faces.Count / 3;
                        int i1, i2, i3;

                        if (line.Contains('/'))
                        {
                            var splitx = split[1].Split('/');
                            var splity = split[2].Split('/');
                            var splitz = split[3].Split('/');

                            i1 = int.Parse(splitx[0]) - 1;
                            i2 = int.Parse(splity[0]) - 1;
                            i3 = int.Parse(splitz[0]) - 1;
                        }
                        else
                        {
                            i1 = int.Parse(split[1]) - 1;
                            i2 = int.Parse(split[2]) - 1;
                            i3 = int.Parse(split[3]) - 1;
                        }

                        faces.Add(new Face(i1, i2, i3));
                        break;

                    default:
                        break;
                }
            }

            var mesh = new Mesh()
            {
                Vertices = [.. vertices],
                Colors = [.. colors],
                Normals = [.. normals],
                Faces = [.. faces],
            };

            return mesh;
        }
    }
}
