using SpanCompression._3rdParty;
using SpanCompression.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SpanCompression.Meshes
{
    public class CoarseMesh
    {
        public Cell[] Cells { get; set; } = [];
        public Face[] Faces { get; set; } = [];

        public CoarseMesh(EdgebreakerOutput edgebreakerOutput, float precision)
        {
            var compressedMesh = ObjLoader.Load(edgebreakerOutput.OutputPath);
            Cells = BuildCells(compressedMesh.Vertices, precision);
            Faces = compressedMesh.Faces;
        }

        private static Cell[] BuildCells(Vector3[] vertices, float precision)
        {
            var cells = new Cell[vertices.Length];
            var size = Vector3.One * precision;

            for (int i = 0; i < cells.Length; i++)
                cells[i] = new Cell(i, vertices[i], size);

            return cells;
        }

        public Mesh ToMesh()
        {
            var vertices = new Vector3[Cells.Length];

            for (int i = 0; i < Cells.Length; i++)
                vertices[i] = Cells[i].Center;

            return new Mesh() { Vertices = vertices, Faces = Faces };
        }

        public Mesh CellsToMesh()
        {
            List<Cuboid> cuboids = [];

            foreach (var cell in Cells)
                cuboids.Add(new Cuboid(cell.Center, cell.Size));

            return Mesh.ConcatenateMeshes([..cuboids]);
        }
    }
}
