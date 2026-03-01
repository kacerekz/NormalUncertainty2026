using SpanCompression.Meshes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SpanCompression.Structures
{
    public class Cell
    {
        public int ID { get; init; }

        public Span SpanX { get; init; }
        public Span SpanY { get; init; }
        public Span SpanZ { get; init; }

        public Span[] Spans => [SpanX, SpanY, SpanZ];

        public Vector3 Center => (Min + Max) * 0.5f;
        public Vector3 Min => new(SpanX.Min, SpanY.Min, SpanZ.Min);
        public Vector3 Max => new(SpanX.Max, SpanY.Max, SpanZ.Max);
        public Vector3 Size => new(SpanX.Size, SpanY.Size, SpanZ.Size);

        public Cell(int id, Vector3 center, Vector3 size)
        {
            ID = id;
            Vector3 half = size / 2f;
            SpanX = new(Axis.X, this, center.X - half.X, center.X + half.X);
            SpanY = new(Axis.Y, this, center.Y - half.Y, center.Y + half.Y);
            SpanZ = new(Axis.Z, this, center.Z - half.Z, center.Z + half.Z);
        }

        public Cell Clone()
        {
            return new Cell(ID, Center, Size);
        }

        public Vector3[] GetCorners()
        {
            float xMin = SpanX.Min;
            float xMax = SpanX.Max;
            float yMin = SpanY.Min;
            float yMax = SpanY.Max;
            float zMin = SpanZ.Min;
            float zMax = SpanZ.Max;

            return
            [
                new(xMin, yMin, zMin), // 0: bottom-back-left
                new(xMax, yMin, zMin), // 1: bottom-back-right
                new(xMax, yMin, zMax), // 2: bottom-front-right
                new(xMin, yMin, zMax), // 3: bottom-front-left

                new(xMin, yMax, zMin), // 4: top-back-left
                new(xMax, yMax, zMin), // 5: top-back-right
                new(xMax, yMax, zMax), // 6: top-front-right
                new(xMin, yMax, zMax), // 7: top-front-left
            ];
        }

        public Vector3 GetRandomPoint()
        {
            float x = SpanX.GetRandomPoint();
            float y = SpanY.GetRandomPoint();
            float z = SpanZ.GetRandomPoint();
            return new Vector3(x, y, z);
        }

        public Span GetSpan(Axis axis)
        {
            return axis switch
            {
                Axis.X => SpanX,
                Axis.Y => SpanY,
                _ => SpanZ
            };
        }
    }
}
