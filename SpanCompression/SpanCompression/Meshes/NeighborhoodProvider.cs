using SpanCompression.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SpanCompression.Meshes
{
    public class NeighborhoodProvider
    {
        public CornerTable cornerTable;

        private int[][] vv;

        private int[][] vf;

        public NeighborhoodProvider(Mesh mesh)
        {
            cornerTable = new CornerTable(mesh.Vertices, mesh.Faces);

            var vertexCount = mesh.Vertices.Length;
            vv = new int[vertexCount][];
            vf = new int[vertexCount][];

            for (int i = 0; i < vertexCount; i++)
            {
                var incident = cornerTable.GetIncident(i);
                vv[i] = incident.Item1;
                vf[i] = incident.Item2;
            }
        }

        public int[] VV(int index)
        {
            return vv[index];
        }

        public int[] VF(int index)
        {
            return vf[index];
        }
    }

    public class CornerTable
    {
        private readonly Vector3[] vertices;
        private readonly Face[] faces;

        public readonly int[] initial;
        public readonly int[] opposite;

        public CornerTable(Vector3[] vertices, Face[] faces)
        {
            this.vertices = vertices;
            this.faces = faces;

            opposite = new int[3 * this.faces.Length];
            initial = new int[this.vertices.Length];

            InitDataStructure();
        }

        public static int GetNextCorner(int corner) =>
            3 * (corner / 3) + (corner + 1) % 3;

        public static int GetPreviousCorner(int corner) =>
            3 * (corner / 3) + (corner + 2) % 3;

        public int GetVertex(int corner) =>
            faces[corner / 3].Indices[corner % 3];

        private void InitDataStructure()
        {
            for (int i = 0; i < opposite.Length; i++)
            {
                opposite[i] = -1;
            }

            // ----- Search for neighbors -----

            var dict = new Dictionary<Edge, int>();

            for (int i = 0; i < faces.Length; i++)
            {
                var f = faces[i];
                var e1 = new Edge(f.i2, f.i3);
                var e2 = new Edge(f.i3, f.i1);
                var e3 = new Edge(f.i1, f.i2);

                Edge[] edges = [e1, e2, e3];

                for (int j = 0; j < edges.Length; j++)
                {
                    Edge e = edges[j];

                    if (dict.ContainsKey(e))
                    {
                        opposite[dict[e]] = 3 * i + j;
                        opposite[3 * i + j] = dict[e];

                        dict.Remove(e);
                    }
                    else
                    {
                        var oe = new Edge(e.v2, e.v1);
                        dict.Add(oe, 3 * i + j);
                    }
                }
            }

            // ----- Initialize first corner -----
            for (int i = 0; i < faces.Length; i++)
            {
                Face f = faces[i];
                initial[f.i1] = 3 * i + 0;
                initial[f.i2] = 3 * i + 1;
                initial[f.i3] = 3 * i + 2;
            }
        }

        public (int[], int[]) GetIncident(int v)
        {
            var resultVV = new List<int>();
            var resultVF = new List<int>();
            var hole = false;

            var current = initial[v];
            current = GetNextCorner(current);
            resultVV.Add(GetVertex(current));
            resultVF.Add(current / 3);

            var first = current;

            while (true)
            {
                if (opposite[current] == -1)
                {
                    current = GetNextCorner(current);
                    hole = true;
                    break;
                }

                current = GetPreviousCorner(opposite[current]);
                if (current == first) break;
                resultVV.Add(GetVertex(current));
                resultVF.Add(current / 3);
            }

            if (!hole)
                return ([.. resultVV], [.. resultVF]);

            // Restart and go the other way this time
            resultVV.Clear();
            resultVF.Clear();

            resultVV.Add(GetVertex(current));
            resultVF.Add(current / 3);

            while (true)
            {
                if (opposite[current] == -1)
                {
                    current = GetPreviousCorner(current);
                    resultVV.Add(GetVertex(current));
                    break;
                }

                current = GetNextCorner(opposite[current]);
                resultVV.Add(GetVertex(current));
                resultVF.Add(current / 3);
            }

            // Get the result in a CCW order
            // to be consistent with the standard pass
            resultVV.Reverse();
            resultVF.Reverse();

            return ([.. resultVV], [.. resultVF]);
        }
    }
}
