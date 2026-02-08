using MyLibrary;
using OpenTK.Mathematics;
using OpenTkRenderer;
using ScottPlot;
using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace NormalUncertainty.Experiments.Convergence
{
    public class Convergence2dExperiment : Experiment
    {
        class Scenario
        {
            public Box2 boundsA;
            public Box2 boundsB;
            public Vector2 averageNormal = Vector2.Zero;
            public List<Vector2> normalHistory = [];
            public int sampleCount;

            GameObject rectObjA;
            GameObject rectObjB;
            GameObject gridObj;
            GameObject unitCircleObj;
            GameObject historyTraceObj;
            GameObject averageVectorObj;

            Vector2 unitCircleOrigin = new Vector2(10.0f, 0.0f);

            public Scene scene;

            public Scenario(Random rnd, int sampleCount = 10_000)
            {
                this.sampleCount = sampleCount;

                rectObjA = new GameObject(null, matRed);
                rectObjB = new GameObject(null, matBlue);
                averageVectorObj = new GameObject(null, matYellow);
                historyTraceObj = new GameObject(null, matTrace);

                var grid = GeometryFactory.CreateCartesianGrid(20, 20);
                gridObj = new GameObject(grid, matWhite);

                var unitCircle = GeometryFactory.CreateCircleMesh(unitCircleOrigin, 1, 180);
                unitCircleObj = new GameObject(unitCircle, matGrey);

                scene = new(cameraManager);
                scene.UseDepthTest = false;
                scene.ClearColor = new Color4(0.05f, 0.05f, 0.05f, 1.0f);

                scene.Add(gridObj);
                scene.Add(unitCircleObj);
                scene.Add(rectObjA);
                scene.Add(rectObjB);
                //scene.Add(historyTraceObj);
                //scene.Add(averageVectorObj);

                // Generate bounds
                float minSize = 0.5f;
                float maxSize = 2.0f;
                float minOffset = 0.5f;
                float maxOffset = 3.0f;

                Vector2 offset = GenerateOffset(rnd, minOffset, maxOffset);
                Vector2 centerA = Vector2.Zero;
                Vector2 centerB = centerA + offset;

                boundsA = GenerateBounds(rnd, centerA, minSize, maxSize);
                boundsB = GenerateBounds(rnd, centerB, minSize, maxSize);

                rectObjA.Mesh = GeometryFactory.CreateBounds(boundsA.Min, boundsA.Max);
                rectObjB.Mesh = GeometryFactory.CreateBounds(boundsB.Min, boundsB.Max);

                // Generate samples
                int samplesTaken = 0;

                while (samplesTaken < sampleCount)
                {
                    // Sample A
                    float tAx = (float)rnd.NextDouble();
                    float tAy = (float)rnd.NextDouble();
                    Vector2 pA = new Vector2(
                        boundsA.Min.X + tAx * (boundsA.Max.X - boundsA.Min.X),
                        boundsA.Min.Y + tAy * (boundsA.Max.Y - boundsA.Min.Y)
                    );

                    // Sample B
                    float tBx = (float)rnd.NextDouble();
                    float tBy = (float)rnd.NextDouble();
                    Vector2 pB = new Vector2(
                        boundsB.Min.X + tBx * (boundsB.Max.X - boundsB.Min.X),
                        boundsB.Min.Y + tBy * (boundsB.Max.Y - boundsB.Min.Y)
                    );

                    // Compute Normal
                    Vector2 line = pB - pA;
                    Vector2 normal = new Vector2(-line.Y, line.X);

                    if (normal.LengthSquared > 0.00001f)
                    {
                        normalHistory.Add(normal);
                        averageNormal += normal;
                        samplesTaken++;
                    }
                }

                averageNormal.Normalize();

                float[] differences = new float[sampleCount];
                Vector2 runningSum = Vector2.Zero;

                for (int i = 0; i < sampleCount; i++)
                {
                    // 1. Add the current normal to the running total
                    runningSum += normalHistory[i];

                    // 2. Clone the sum to normalize without affecting the total
                    Vector2 tempAvg = runningSum;
                    tempAvg.Normalize();

                    // 3. Convert to Numerics for your math utility
                    System.Numerics.Vector3 a = new(tempAvg.X, tempAvg.Y, 0f);
                    System.Numerics.Vector3 b = new(averageNormal.X, averageNormal.Y, 0f);

                    // 4. Calculate angular difference
                    differences[i] = MathUtil.UnsignedUnitVectorAngularDifferenceFast(a, b);
                }

                PlotAngularDifferences(differences);
            }

            public void PlotAngularDifferences(float[] differences)
            {
                // 1. Initialize the Plot
                Plot myPlot = new();

                // 2. Prepare the data
                // Create an X-axis array: [0.0, 1.0, 2.0, ... differences.Length - 1]
                double[] dataX = Generate.Consecutive(differences.Length);

                // Explicitly convert float[] to double[] for the Y-axis
                double[] dataY = differences.Select(x => (double)x).ToArray();

                // 3. Add the Scatter plot using both X and Y arrays
                var scatter = myPlot.Add.Scatter(dataX, dataY);

                // 4. Style as a line only
                scatter.LineWidth = 2;
                scatter.MarkerSize = 0; // This hides the individual dots, leaving just the line
                scatter.Color = Colors.Blue;

                // 5. Style the Axes
                myPlot.XLabel("Sample Count");
                myPlot.YLabel("Angular Difference");
                myPlot.Title("Angular Difference over Samples");

                // 6. Save and Open
                string filePath = System.IO.Path.GetFullPath("convergence_2D.png");
                myPlot.SavePng(filePath, 800, 600);

                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }

            private Box2 GenerateBounds(Random rnd, Vector2 centerA, float minSize, float maxSize)
            {
                float x = (float)rnd.NextDouble() * (maxSize - minSize) + minSize;
                float y = (float)rnd.NextDouble() * (maxSize - minSize) + minSize;
                Vector2 sizeA = new(x, y);
                return new Box2(centerA - sizeA / 2, centerA + sizeA / 2);
            }

            private static Vector2 GenerateOffset(Random rnd, float minOffset, float maxOffset)
            {
                float angle = (float)(rnd.NextDouble() * Math.PI * 2);
                Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                Vector2 offset = dir * (float)(minOffset + rnd.NextDouble() * (maxOffset - minOffset));
                return offset;
            }
        }

        static int seed = 42;

        public static CameraManager cameraManager;

        public static ColorMaterial matRed;
        public static ColorMaterial matBlue;
        public static ColorMaterial matYellow;
        public static ColorMaterial matTrace;
        public static ColorMaterial matWhite;
        public static ColorMaterial matGrey;
        public static ColorMaterial matLightGrey;
        public static ColorMaterial matDarkGrey;

        public override void Initialize(int width, int height)
        {
            InitializeCamera(width, height);
            InitializeMaterials();

            Random rand = new();
            Scenario scenario = new(rand);
            Scene = scenario.scene;
        }

        private static void InitializeMaterials()
        {
            // 2. Load Basic Shaders
            var shader = new Shader("Shaders/test.vert", "Shaders/test.frag");
            AssetManager.AddShader("default", shader);

            // 3. Create Materials
            matRed = new ColorMaterial(shader, new Vector4(1, 0.2f, 0.2f, 1));
            matBlue = new ColorMaterial(shader, new Vector4(0.2f, 0.2f, 1, 1));
            matYellow = new ColorMaterial(shader, new Vector4(1, 1, 0, 1));
            matTrace = new ColorMaterial(shader, new Vector4(0, 1, 1, 0.5f));
            matWhite = new ColorMaterial(shader, Vector4.One);
            matGrey = new ColorMaterial(shader, new Vector4(0.3f, 0.3f, 0.3f, 1));
            matLightGrey = new ColorMaterial(shader, new Vector4(0.5f, 0.5f, 0.5f, 1));
            matDarkGrey = new ColorMaterial(shader, new Vector4(0.2f, 0.2f, 0.2f, 1));
        }

        private void InitializeCamera(int width, int height)
        {
            // 1. Setup Camera (Ortho, zoomed out to see both scene and visualization)
            var orthoCam = new OrthoCamera(width / (float)height);
            orthoCam.Zoom = 5.0f;
            orthoCam.Position = new Vector2(2.0f, 0.0f); // Center camera between scene and vis
            cameraManager = new CameraManager(orthoCam);
        }
    }
}
