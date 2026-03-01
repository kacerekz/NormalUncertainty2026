using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SpanCompression.Structures
{
    public class Aabb
    {
        public float[,] Box = new float[2, 3];

        public float MinX
        {
            get { return Box[0, 0]; }
            set { Box[0, 0] = value; }
        }

        public float MinY
        {
            get { return Box[0, 1]; }
            set { Box[0, 1] = value; }
        }

        public float MinZ
        {
            get { return Box[0, 2]; }
            set { Box[0, 2] = value; }
        }

        public float MaxX
        {
            get { return Box[1, 0]; }
            set { Box[1, 0] = value; }
        }

        public float MaxY
        {
            get { return Box[1, 1]; }
            set { Box[1, 1] = value; }
        }

        public float MaxZ
        {
            get { return Box[1, 2]; }
            set { Box[1, 2] = value; }
        }

        public float SizeX => MaxX - MinX;

        public float SizeY => MaxY - MinY;

        public float SizeZ => MaxZ - MinZ;

        public Vector3 Center => (Min + Max) * 0.5f;

        public Vector3 Min => new(MinX, MinY, MinZ);

        public Vector3 Max => new(MaxX, MaxY, MaxZ);

        public Aabb(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
        {
            MinX = minX;
            MinY = minY;
            MinZ = minZ;

            MaxX = maxX;
            MaxY = maxY;
            MaxZ = maxZ;
        }

        public Aabb(Vector3 min, Vector3 max)
            : this(min.X, min.Y, min.Z, max.X, max.Y, max.Z)
        { }

        public override string ToString() =>
        $"Min = {Min.X:F9}, {Min.Y:F9}, {Min.Z:F9}\n" +
        $"Max = {Max.X:F9}, {Max.Y:F9}, {Max.Z:F9}";
    }
}
