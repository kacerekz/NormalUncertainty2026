using SpanCompression.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SpanCompression.Meshes
{
    public class Cuboid : Mesh
    {
        public Cuboid(Vector3 center, Vector3 size)
        {
            Vector3 half = size / 2f;

            // Define 8 vertices relative to the center
            Vertices =
            [
            // Bottom face
            center + new Vector3(-half.X, -half.Y, -half.Z),
            center + new Vector3( half.X, -half.Y, -half.Z),
            center + new Vector3( half.X, -half.Y,  half.Z),
            center + new Vector3(-half.X, -half.Y,  half.Z),

            // Top face
            center + new Vector3(-half.X,  half.Y, -half.Z),
            center + new Vector3( half.X,  half.Y, -half.Z),
            center + new Vector3( half.X,  half.Y,  half.Z),
            center + new Vector3(-half.X,  half.Y,  half.Z),
            ];

            Lines =
            [
            (0, 1), (1, 2), (2, 3), (3, 0), // bottom
            (4, 5), (5, 6), (6, 7), (7, 4), // top
            (0, 4), (1, 5), (2, 6), (3, 7), // sides
            ];

            //// Triangle indices for each of the 6 box faces (2 triangles per face)
            //var indices = new int[]
            //{
            //// Bottom face
            //0, 1, 2,  0, 2, 3,

            //// Top face
            //4, 6, 5,  4, 7, 6,

            //// Front face
            //3, 2, 6,  3, 6, 7,

            //// Back face
            //0, 5, 1,  0, 4, 5,

            //// Left face
            //0, 3, 7,  0, 7, 4,

            //// Right face
            //1, 5, 6,  1, 6, 2,
            //};

            //Faces = new Face[indices.Length/ 3];
            //for (int i = 0; i < indices.Length; i += 3)
            //{
            //    Faces[i / 3] = new Face(indices[i], indices[i + 1], indices[i + 2]);
            //}
        }
    }
}
