using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;

namespace OpenTkRenderer
{
    public static class GeometryFactory
    {
        public static Mesh CreateCircleMesh(Vector2 center, float radius, int segments)
        {
            List<Vertex> verts = new List<Vertex>();
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * MathF.PI * 2;
                float x = center.X + MathF.Cos(angle) * radius;
                float y = center.Y + MathF.Sin(angle) * radius;
                verts.Add(new Vertex(new Vector3(x, y, 0), Vector3.One, Vector2.Zero));
            }
            return new Mesh(verts.ToArray(), PrimitiveType.LineLoop);
        }

        // Grid with colored axes (Vertex Colors)
        public static Mesh CreateCartesianGrid(int rangeX, int rangeY)
        {
            List<Vertex> vertices = new List<Vertex>();
            Vector3 gridColor = new Vector3(0.2f, 0.2f, 0.2f); // Dim grey
            Vector3 axisColor = new Vector3(0.3f, 0.3f, 0.5f); // Bright grey/white

            for (int x = -rangeX; x <= rangeX; x++)
            {
                Vector3 color = (x == 0) ? axisColor : gridColor;
                vertices.Add(new Vertex(new Vector3(x, -rangeY, 0), color, Vector2.Zero));
                vertices.Add(new Vertex(new Vector3(x, rangeY, 0), color, Vector2.Zero));
            }

            for (int y = -rangeY; y <= rangeY; y++)
            {
                Vector3 color = (y == 0) ? axisColor : gridColor;
                vertices.Add(new Vertex(new Vector3(-rangeX, y, 0), color, Vector2.Zero));
                vertices.Add(new Vertex(new Vector3(rangeX, y, 0), color, Vector2.Zero));
            }

            return new Mesh(vertices.ToArray(), PrimitiveType.Lines);
        }

        // Bounds defined by two points
        public static Mesh CreateBounds(Vector2 bottomLeft, Vector2 topRight)
        {
            Vertex[] vertices = {
                new Vertex(new Vector3(bottomLeft.X, bottomLeft.Y, 0), Vector3.One, Vector2.Zero),
                new Vertex(new Vector3(topRight.X,   bottomLeft.Y, 0), Vector3.One, Vector2.Zero),
                new Vertex(new Vector3(topRight.X,   topRight.Y,   0), Vector3.One, Vector2.Zero),
                new Vertex(new Vector3(bottomLeft.X, topRight.Y,   0), Vector3.One, Vector2.Zero)
            };
            return new Mesh(vertices, PrimitiveType.LineLoop);
        }

        // Sample quad defined by center and side length
        public static Mesh CreateSample(Vector2 center, float side)
        {
            float half = side / 2.0f;
            Vertex[] vertices = {
                new Vertex(new Vector3(center.X - half, center.Y - half, 0), Vector3.One, Vector2.Zero),
                new Vertex(new Vector3(center.X + half, center.Y - half, 0), Vector3.One, Vector2.Zero),
                new Vertex(new Vector3(center.X + half, center.Y + half, 0), Vector3.One, Vector2.Zero),

                new Vertex(new Vector3(center.X - half, center.Y - half, 0), Vector3.One, Vector2.Zero),
                new Vertex(new Vector3(center.X + half, center.Y + half, 0), Vector3.One, Vector2.Zero),
                new Vertex(new Vector3(center.X - half, center.Y + half, 0), Vector3.One, Vector2.Zero)
            };
            return new Mesh(vertices, PrimitiveType.Triangles);
        }
    }
}