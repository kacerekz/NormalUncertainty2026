using MyLibrary;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace NormalUncertainty.Experiments.Convergence
{
    public class ConvergencePreliminaries
    {
        private static void TestCircle()
        {
            Vector3 reference;
            Vector3 current;

            float step = 10f;
            float shift = 90f;

            reference = new Vector3(0f, 1f, 0f);

            for (float angle = 0; angle < 360; angle += step)
            {
                float angleR = MathUtil.ToRadians(angle + shift);

                current = new Vector3(
                    MathF.Cos(angleR),
                    MathF.Sin(angleR),
                    0f);

                float differenceR = MathUtil.UnsignedUnitVectorAngularDifferenceFast(reference, current);
                float difference = MathUtil.ToDegrees(differenceR);
                difference = MathF.Round(difference, 0);
                Console.WriteLine(difference);
            }
        }

        private static void TestSphere()
        {
            Vector3 reference;
            Vector3 current;

            reference = new Vector3(0f, 1f, 0f);
            var points = GenerateSpherePoints(100);
            var size = 0.1f;

            var quad = CreateDirectedQuad(reference, reference, size);
            var blue = new Vector3(0f, 0f, 1f);
            var referenceQuad = new Mesh()
            {
                Vertices = quad.Vertices,
                Faces = quad.Faces,
                Colors = [blue, blue, blue, blue]
            };

            var meshes = new List<Mesh>();
            meshes.Add(referenceQuad);

            for (int i = 0; i < points.Count; i++)
            {
                current = points[i];

                var differenceR = MathUtil.UnsignedUnitVectorAngularDifferenceFast(reference, current);
                var currentQuad = CreateDirectedQuad(current, current, size);
                var currentColor = GetAngleColor(differenceR);

                Mesh currentMesh = new()
                {
                    Vertices = currentQuad.Vertices,
                    Faces = currentQuad.Faces,
                    Colors = [currentColor, currentColor, currentColor, currentColor]
                };

                meshes.Add(currentMesh);
            }

            var concat = Mesh.ConcatenateMeshes(meshes.ToArray());
            concat.Write("test3d.obj");
            Util.OpenWithMeshLab("test3d.obj");
        }

        public static Vector3 GetAngleColor(float angleRad)
        {
            if (angleRad < 0f)
                return new Vector3(0f, 1f, 0f);

            // Define our target colors
            Vector3 red = new Vector3(1f, 0f, 0f);
            Vector3 white = new Vector3(1f, 1f, 1f);

            // 1. Calculate t: 0.0 at 0 radians, 1.0 at PI radians (180°)
            // We use Math.Abs so that negative angles (if any) are treated the same way.
            float t = Math.Clamp(angleRad / MathF.PI, 0f, 1f);

            // 2. Linearly interpolate between Red and White
            return Vector3.Lerp(red, white, t);
        }

        public static (Vector3[] Vertices, Face[] Faces) CreateDirectedQuad(Vector3 direction, Vector3 center, float size)
        {
            // 1. Ensure the direction is a unit vector
            Vector3 normal = Vector3.Normalize(direction);

            // 2. Find an "Up" vector that isn't parallel to our normal
            // If the normal is pointing mostly Up, we use Forward instead.
            Vector3 up = Math.Abs(normal.Y) > 0.9f ? new Vector3(0, 0, 1) : new Vector3(0, 1, 0);

            // 3. Create a local coordinate system (Orthonormal Basis)
            Vector3 right = Vector3.Normalize(Vector3.Cross(up, normal));
            Vector3 localUp = Vector3.Cross(normal, right);

            // 4. Calculate corner offsets
            float halfSize = size * 0.5f;
            Vector3 r = right * halfSize;
            Vector3 u = localUp * halfSize;

            // 5. Generate the 4 vertices
            Vector3[] vertices = new Vector3[4]
            {
                center - r - u, // Bottom Left
                center + r - u, // Bottom Right
                center + r + u, // Top Right
                center - r + u  // Top Left
            };

            // 6. Define the 2 triangles (Indices for OBJ/Graphics)
            // Triangle 1: 0, 1, 2 | Triangle 2: 0, 2, 3
            Face[] indices = [new Face(0, 1, 2), new Face(0, 2, 3)];

            return (vertices, indices);
        }

        public static List<Vector3> GenerateSpherePoints(int n)
        {
            List<Vector3> points = new List<Vector3>();
            float phi = MathF.PI * (MathF.Sqrt(5f) - 1f); // Golden ratio increment

            for (int i = 0; i < n; i++)
            {
                // y goes from 1 to -1
                float y = 1 - (i / (float)(n - 1)) * 2;
                float radius = MathF.Sqrt(1 - y * y); // Radius at y

                float theta = phi * i; // Golden angle increment

                float x = MathF.Cos(theta) * radius;
                float z = MathF.Sin(theta) * radius;

                points.Add(new Vector3(x, y, z));
            }
            return points;
        }
    }
}
