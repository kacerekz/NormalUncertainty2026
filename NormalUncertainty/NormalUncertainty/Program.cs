
using MyLibrary;
using OpenTkRenderer;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using System.Reflection;

namespace NormalUncertainty
{
    internal class Program
    {
        private static readonly Random _random = new Random();

        static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            // Make 2 example quads
            // Generate points within them randomly (use same method as original project) (consult AI for alternatives)
            // Connect point pair into line, calculate its normal
            // Update average normal. Update average deviation from average normal?
            // Plot the CHANGE in average deviation as new samples are added.
            // Observe it decreasing?

            int width = 800;
            int height = 600;

            // This line creates a new instance, and wraps the instance in a using statement so it's automatically disposed once we've exited the block.
            using (Game game = new Game(width, height, "Normal Uncertainty"))
            {
                var orthoCam = new OrthoCamera(width / (float)height);
                orthoCam.Zoom = 1.0f; // Zoom out a bit to see the whole triangle
                orthoCam.Position = new OpenTK.Mathematics.Vector2(0, 0);
                var orbitCam = new OrbitCamera(width / (float)height);
                var camera = new CameraManager(orthoCam);


                Scene scene = new Scene();
                Shader shader = new Shader("Shaders/test.vert", "Shaders/test.frag");

                // Now building the scene is just "adding recipes"
                var grid = new GameObject(
                    GeometryFactory.CreateCartesianGrid(10, 10),
                    new ColorMaterial(shader, OpenTK.Mathematics.Vector4.One)
                );

                var bounds = new GameObject(
                    GeometryFactory.CreateBounds(new OpenTK.Mathematics.Vector2(-2, -2), new OpenTK.Mathematics.Vector2(4, 3)),
                    new ColorMaterial(shader, new OpenTK.Mathematics.Vector4(1.0f, 1.0f, 1.0f, 1.0f))
                );

                var sample = new GameObject(
                    GeometryFactory.CreateSample(new OpenTK.Mathematics.Vector2(2, 2), 0.1f),
                    new ColorMaterial(shader, new OpenTK.Mathematics.Vector4(1, 0.5f, 0, 1))
                );

                scene.Add(grid);
                scene.Add(bounds);
                scene.Add(sample);

                game._activeScene = scene;
                game._cameraManager = camera;
                game.Run();
            }

        }
    }
}
