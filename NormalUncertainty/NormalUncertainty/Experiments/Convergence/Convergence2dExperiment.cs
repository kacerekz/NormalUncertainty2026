using OpenTK.Mathematics;
using OpenTkRenderer;

namespace NormalUncertainty.Experiments
{
    public class Convergence2dExperiment : Experiment
    {
        int seed = 42;

        // Simulation State
        private Box2 _boundsA;
        private Box2 _boundsB;
        private Vector2 _accumulatedNormal;
        private long _sampleCount;
        private List<Vector3> _historyPoints = new List<Vector3>();
        private const int MAX_HISTORY = 1000; // Keep the tail manageable

        // Visual Elements
        private GameObject _rectObjA;
        private GameObject _rectObjB;
        private GameObject _averageVectorObj; // The "Current" arrow
        private GameObject _historyTraceObj;  // The path of convergence
        private GameObject _unitCircleObj;    // Background for the visualization

        // Settings
        private Vector2 _visOrigin = new Vector2(4.0f, 0.0f); // Where to draw the circle
        private float _visScale = 1.5f; // Radius of the visualization circle

        public override void Initialize(int width, int height)
        {
            // 1. Setup Camera (Ortho, zoomed out to see both scene and visualization)
            var orthoCam = new OrthoCamera(width / (float)height);
            orthoCam.Zoom = 5.0f;
            orthoCam.Position = new Vector2(2.0f, 0.0f); // Center camera between scene and vis
            Scene = new Scene(new CameraManager(orthoCam));
            Scene.UseDepthTest = false;
            Scene.ClearColor = new Color4(0.05f, 0.05f, 0.05f, 1.0f);

            // 2. Load Basic Shaders
            var shader = new Shader("Shaders/test.vert", "Shaders/test.frag");
            AssetManager.AddShader("default", shader);

            // 3. Create Materials
            var matRed = new ColorMaterial(shader, new Vector4(1, 0.2f, 0.2f, 1));
            var matBlue = new ColorMaterial(shader, new Vector4(0.2f, 0.2f, 1, 1));
            var matYellow = new ColorMaterial(shader, new Vector4(1, 1, 0, 1));
            var matTrace = new ColorMaterial(shader, new Vector4(0, 1, 1, 0.5f));
            var matGrey = new ColorMaterial(shader, new Vector4(0.3f, 0.3f, 0.3f, 1));

            // 4. Initialize Placeholder Objects (Meshes will be updated in GenerateScenario)
            _rectObjA = new GameObject(null, matRed);
            _rectObjB = new GameObject(null, matBlue);

            // Visualization Objects
            _averageVectorObj = new GameObject(null, matYellow);
            _historyTraceObj = new GameObject(null, matTrace);
            _unitCircleObj = new GameObject(GeometryFactory.CreateSample(_visOrigin, _visScale * 2), matGrey); // Placeholder quad -> Circle later?

            // Allow drawing LineLoops/Strips
            // Note: Since GeometryFactory creates Triangles/Lines, we might need custom mesh gen here

            Scene.Add(_rectObjA);
            Scene.Add(_rectObjB);
            Scene.Add(_unitCircleObj);
            Scene.Add(_historyTraceObj);
            Scene.Add(_averageVectorObj);

            // 5. Start first run
            GenerateScenario();
        }

        private void GenerateScenario()
        {
            // Reset State
            _accumulatedNormal = Vector2.Zero;
            _sampleCount = 0;
            _historyPoints.Clear();

            // --- Neighbor-Based Generation Logic ---
            var rnd = new Random(seed);

            // 1. Anchor Rect (Centered near 0,0)
            Vector2 sizeA = new Vector2((float)rnd.NextDouble() + 0.5f, (float)rnd.NextDouble() + 0.5f); // 0.5 to 1.5
            Vector2 centerA = Vector2.Zero;
            _boundsA = new Box2(centerA - sizeA / 2, centerA + sizeA / 2);

            // 2. Neighbor Rect
            // Direction 0 to 2PI
            float angle = (float)(rnd.NextDouble() * Math.PI * 2);
            Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));

            // Gap (-0.2 overlap to 1.5 distant)
            float gap = (float)(rnd.NextDouble() * 1.7 - 0.2);

            // Approximate radius to place 'outside'
            float radiusA = Math.Max(sizeA.X, sizeA.Y) / 2;
            Vector2 sizeB = new Vector2((float)rnd.NextDouble() + 0.5f, (float)rnd.NextDouble() + 0.5f);
            float radiusB = Math.Max(sizeB.X, sizeB.Y) / 2;

            Vector2 centerB = centerA + dir * (radiusA + radiusB + gap);
            _boundsB = new Box2(centerB - sizeB / 2, centerB + sizeB / 2);

            // 3. Update Visuals (Disposing old meshes to avoid leaks!)
            if (_rectObjA.Mesh != null) _rectObjA.Mesh.Dispose();
            if (_rectObjB.Mesh != null) _rectObjB.Mesh.Dispose();

            _rectObjA.Mesh = GeometryFactory.CreateBounds(_boundsA.Min, _boundsA.Max);
            _rectObjB.Mesh = GeometryFactory.CreateBounds(_boundsB.Min, _boundsB.Max);

            // Update Unit Circle Visual (Fixed Origin)
            if (_unitCircleObj.Mesh != null) _unitCircleObj.Mesh.Dispose();
            _unitCircleObj.Mesh = CreateCircleMesh(_visOrigin, _visScale, 64);
        }


        public override void Update(double deltaTime)
        {
            // Reset trigger (Press 'R')
            //var keyboard = Scene.CameraManager.ActiveCamera is Camera c ? null : null; // Accessing input is tricky in current arch without passing it down
            // For now, we'll just run. To reset, we might need to hook into the Game class input or rely on auto-reset.

            // --- Sampling Loop (Speed up: 50 samples per frame) ---
            var rnd = new Random();
            for (int i = 0; i < 50; i++)
            {
                // Sample A
                float tAx = (float)rnd.NextDouble();
                float tAy = (float)rnd.NextDouble();
                Vector2 pA = new Vector2(
                    _boundsA.Min.X + tAx * (_boundsA.Max.X - _boundsA.Min.X),
                    _boundsA.Min.Y + tAy * (_boundsA.Max.Y - _boundsA.Min.Y)
                );

                // Sample B
                float tBx = (float)rnd.NextDouble();
                float tBy = (float)rnd.NextDouble();
                Vector2 pB = new Vector2(
                    _boundsB.Min.X + tBx * (_boundsB.Max.X - _boundsB.Min.X),
                    _boundsB.Min.Y + tBy * (_boundsB.Max.Y - _boundsB.Min.Y)
                );

                // Compute Normal
                Vector2 line = pB - pA;
                // Rotate 90 degrees: (x, y) -> (-y, x)
                Vector2 normal = new Vector2(-line.Y, line.X);

                // Handle zero-length (rare but possible with overlap)
                if (normal.LengthSquared > 0.00001f)
                {
                    normal = Vector2.Normalize(normal);

                    // Flip logic: ensure normals point roughly in same hemisphere for averaging
                    // (Simple dot product check against previous average)
                    if (_sampleCount > 0)
                    {
                        if (Vector2.Dot(normal, Vector2.Normalize(_accumulatedNormal)) < 0)
                        {
                            normal = -normal;
                        }
                    }

                    _accumulatedNormal += normal;
                    _sampleCount++;
                }
            }

            // --- Update Visualization ---
            if (_sampleCount > 0)
            {
                Vector2 avgNorm = Vector2.Normalize(_accumulatedNormal);

                // 1. Current Average Vector Arrow
                if (_averageVectorObj.Mesh != null) _averageVectorObj.Mesh.Dispose();

                // Draw line from Origin to Origin + Normal * Scale
                Vector3 start = new Vector3(_visOrigin.X, _visOrigin.Y, 0);
                Vector3 end = start + new Vector3(avgNorm.X, avgNorm.Y, 0) * _visScale;

                // Simple single line mesh
                Vertex[] lineVerts = {
                    new Vertex(start, Vector3.One, Vector2.Zero),
                    new Vertex(end, Vector3.One, Vector2.Zero)
                };
                _averageVectorObj.Mesh = new Mesh(lineVerts, OpenTK.Graphics.OpenGL4.PrimitiveType.Lines);

                // 2. History Trace
                // Only add to history if it changed enough or every N frames to save memory
                // For simplicity: Add every frame
                _historyPoints.Add(end);

                // Keep history limited
                if (_historyPoints.Count > MAX_HISTORY) _historyPoints.RemoveAt(0);

                if (_historyTraceObj.Mesh != null) _historyTraceObj.Mesh.Dispose();

                // Convert history list to Vertex Array
                Vertex[] traceVerts = new Vertex[_historyPoints.Count];
                for (int k = 0; k < _historyPoints.Count; k++)
                {
                    traceVerts[k] = new Vertex(_historyPoints[k], Vector3.One, Vector2.Zero);
                }

                // Render as LineStrip
                if (traceVerts.Length > 1)
                {
                    _historyTraceObj.Mesh = new Mesh(traceVerts, OpenTK.Graphics.OpenGL4.PrimitiveType.LineStrip);
                }
            }

            // Input handling for Reset (Optional hack: check specific keys via static OpenTK state if accessible, 
            // or pass input down. Since Input is in Game.cs, let's assume auto-reset for now isn't needed unless requested).
        }

        // Helper for the Circle background
        private Mesh CreateCircleMesh(Vector2 center, float radius, int segments)
        {
            List<Vertex> verts = new List<Vertex>();
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * MathF.PI * 2;
                float x = center.X + MathF.Cos(angle) * radius;
                float y = center.Y + MathF.Sin(angle) * radius;
                verts.Add(new Vertex(new Vector3(x, y, 0), Vector3.One, Vector2.Zero));
            }
            return new Mesh(verts.ToArray(), OpenTK.Graphics.OpenGL4.PrimitiveType.LineLoop);
        }
    }
}