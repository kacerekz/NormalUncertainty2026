using SpanCompression.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SpanCompression
{
    public interface ISampler
    {
        Dictionary<int, Vector3[]> Sample(Cell[] cells);
        Vector3[] Sample(Cell cells);
    }

    public class CornerSampler : ISampler
    {
        public Dictionary<int, Vector3[]> Sample(Cell[] cells)
        {
            Dictionary<int, Vector3[]> samples = [];
            for (int i = 0; i < cells.Length; i++)
                samples[cells[i].ID] = Sample(cells[i]);
            return samples;
        }

        public Vector3[] Sample(Cell cells)
        {
            return cells.GetCorners();
        }

        public override string ToString()
        {
            return "Corner sampler";
        }
    }

    public class RandomSampler(int sampleCount) : ISampler
    {
        public int SampleCount { get; init; } = sampleCount;

        public Dictionary<int, Vector3[]> Sample(Cell[] cells)
        {
            Dictionary<int, Vector3[]> samples = [];

            for (int i = 0; i < cells.Length; i++)
            {
                samples[cells[i].ID] = Sample(cells[i]);
            }
            
            return samples;
        }

        public Vector3[] Sample(Cell cell)
        {
            List<Vector3> points = [];

            for (int j = 0; j < SampleCount; j++)
            {
                var point = cell.GetRandomPoint();
                points.Add(point);
            }

            return [.. points];
        }

        public override string ToString()
        {
            return "Random sampler, n = " + SampleCount;
        }
    }
}
