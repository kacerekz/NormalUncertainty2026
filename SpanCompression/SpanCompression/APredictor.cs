using SpanCompression.Meshes;
using SpanCompression.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SpanCompression
{
    public abstract class APredictor
    {
        public List<int> Corrections { get; } = [];
        public List<int> Predictions { get; } = [];
        public List<int> Truth { get; } = [];

        public abstract int Predict(Span span);

        public void Correct(int prediction, int truth)
        {
            if (prediction == truth)
            {
                Corrections.Add(0);
                Truth.Add(prediction);
            }
            else
            {
                Corrections.Add(1);
                Truth.Add(prediction == 0 ? 1 : 0);
            }
        }

        public void Write(string path)
        {
            using var sw = new StreamWriter(path);
            
            for (int i = 0; i < Corrections.Count; i++)
            {
                sw.Write(Corrections[i] + ";");
                sw.Write(Predictions[i] + ";");
                sw.Write(Truth[i] + ";");
                sw.WriteLine();
            }
        }

        public bool[] GetBooleans()
        {
            bool[] corrections = new bool[Corrections.Count];
            for (int i = 0; i < Corrections.Count; i++)
                corrections[i] = Corrections[i] == 0;
            return corrections;
        }
    }

    public class SimplePredictor(CoarseMesh coarseMesh, NeighborhoodProvider neighborhoodProvider) : APredictor
    {
        private CoarseMesh coarseMesh = coarseMesh;
        private NeighborhoodProvider neighborhoodProvider = neighborhoodProvider;

        public override int Predict(Span span)
        {
            var id = span.parent.ID;
            var vv = neighborhoodProvider.VV(id);

            var sum = Vector3.Zero;

            for (int i = 0; i < vv.Length; i++)
                sum += coarseMesh.Cells[vv[i]].Center;

            var avg = sum / vv.Length;

            var prediction = span.axis switch
            {
                Axis.X => avg.X < span.Center ? 0 : 1,
                Axis.Y => avg.Y < span.Center ? 0 : 1,
                _ => avg.Z < span.Center ? 0 : 1,
            };

            Predictions.Add(prediction);

            return prediction;
        }

        public override string ToString()
        {
            return "Simple predictor";
        }
    }

    public class ParallelogramAveragePredictor(CoarseMesh coarseMesh, CornerTable cornerTable) : APredictor
    {
        private CoarseMesh coarseMesh = coarseMesh;
        private CornerTable cornerTable = cornerTable;

        public override int Predict(Span span)
        {
            int id = span.parent.ID;
            List<int> add = []; // vertices to add
            List<int> sub = []; // vertices to subtract

            GatherVertices(id, add, sub, cornerTable);

            Vector3 avg = Vector3.Zero;

            foreach (var i in add)
            {
                var v = coarseMesh.Cells[i].Center;
                avg += 2 * v;
            }

            foreach (var i in sub)
            {
                var v = coarseMesh.Cells[i].Center;
                avg -= v;
            }

            avg *= 1f / add.Count;

            var prediction =  span.axis switch
            {
                Axis.X => avg.X < span.Center ? 0 : 1,
                Axis.Y => avg.Y < span.Center ? 0 : 1,
                _ => avg.Z < span.Center ? 0 : 1,
            };

            Predictions.Add(prediction);

            return prediction;
        }

        private static void GatherVertices(int id, List<int> add, List<int> sub, CornerTable ct)
        {
            // Walk a circle around this vertex
            // The VV will be the vertices to add (2x)
            // The opposite vertex over each triangle will be the subtracted vertex
            int current = ct.initial[id];
            sub.Add(ct.GetVertex(ct.opposite[current]));
            current = CornerTable.GetNextCorner(current);
            add.Add(ct.GetVertex(current));

            int first = current;
            bool hole = false;

            while (true)
            {
                if (ct.opposite[current] == -1)
                {
                    current = CornerTable.GetNextCorner(current);
                    hole = true;
                    break;
                }

                current = CornerTable.GetPreviousCorner(ct.opposite[current]);
                if (current == first) break;
                sub.Add(ct.GetVertex(ct.opposite[CornerTable.GetPreviousCorner(current)]));
                add.Add(ct.GetVertex(current));
            }

            if (!hole) return;

            sub.Clear();
            add.Clear();

            var currentInitCorner = CornerTable.GetNextCorner(current);
            sub.Add(ct.GetVertex(ct.opposite[currentInitCorner]));
            add.Add(ct.GetVertex(current));

            while (true)
            {
                if (ct.opposite[current] == -1)
                {
                    current = CornerTable.GetPreviousCorner(current);
                    add.Add(ct.GetVertex(current));
                    break;
                }

                current = CornerTable.GetNextCorner(ct.opposite[current]);
                currentInitCorner = CornerTable.GetNextCorner(current);
                sub.Add(ct.GetVertex(ct.opposite[currentInitCorner]));
                add.Add(ct.GetVertex(current));
            }

            if (add.Count != sub.Count * 2)
                throw new Exception("Unexpected number of vertices gathered: something went wrong.");
        }

        public override string ToString()
        {
            return "Parallelogram average predictor";
        }
    }
}
