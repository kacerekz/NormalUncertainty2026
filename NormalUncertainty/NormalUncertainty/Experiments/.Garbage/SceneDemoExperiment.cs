using OpenTK.Mathematics;
using OpenTkRenderer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NormalUncertainty.Experiments.Convergence
{
    internal class SceneDemoExperiment : Experiment
    {
        public override void Initialize(int width, int height)
        {
            var orthoCam = new OrthoCamera(width / (float)height);
            orthoCam.Zoom = 2.0f;
            Scene = new Scene(new CameraManager(orthoCam));

            // Set experiment-specific OpenGL state 
            Scene.UseDepthTest = false;
            Scene.ClearColor = new Color4(0.05f, 0.05f, 0.05f, 1.0f);

            // Register Assets
            var shader = new Shader("Shaders/test.vert", "Shaders/test.frag");

            var sampleMesh = GeometryFactory.CreateSample(Vector2.Zero, 1.0f);
            var sampleMaterial = new ColorMaterial(shader, new Vector4(1, 0.5f, 0, 1));

            var gridMesh = GeometryFactory.CreateCartesianGrid(20, 20);
            var gridMaterial = new ColorMaterial(shader, Vector4.One);
            var grid = new GameObject(gridMesh, gridMaterial);

            var boundsMesh = GeometryFactory.CreateBounds(new Vector2(-5, -5), new Vector2(5, 5));
            var boundsMaterial = new ColorMaterial(shader, new Vector4(0, 1, 0, 1));
            var bounds = new GameObject(boundsMesh, boundsMaterial);

            AssetManager.AddShader("default", shader);

            AssetManager.AddMesh("grid_mesh", gridMesh);
            AssetManager.AddMesh("bounds_mesh", boundsMesh);
            AssetManager.AddMesh("sample_mesh", sampleMesh);

            AssetManager.AddMaterial("grid_mat", gridMaterial);
            AssetManager.AddMaterial("bounds_mat", boundsMaterial);
            AssetManager.AddMaterial("sample_mat", sampleMaterial);

            // Add persistent background elements
            Scene.Add(grid);
            Scene.Add(bounds);

            // Add an initial sample to test the quad you mentioned was missing
            AddSample(new Vector2(2, 2), 0.2f);
        }

        public void AddSample(Vector2 position, float size)
        {
            var sample = new GameObject(
                AssetManager.GetMesh("sample_mesh"),
                AssetManager.GetMaterial("sample_mat"));

            // We use the Transform matrix to place the unit quad 
            sample.Transform = Matrix4.CreateScale(size) * Matrix4.CreateTranslation(position.X, position.Y, 0);

            Scene.Add(sample);
        }

        public override void Update(double deltaTime)
        {
            // This is where we will eventually add your random point logic!
        }
    }
}
