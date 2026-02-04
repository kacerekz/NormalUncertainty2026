using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace OpenTkRenderer
{
    public class OrthoCamera : Camera
    {
        public Vector2 Position = Vector2.Zero;
        public float Zoom = 1.0f;

        // Specialized Settings
        public float PanSensitivity = 0.002f;
        public float ZoomSensitivity = 0.1f;

        public OrthoCamera(float aspectRatio) : base(aspectRatio) { }

        protected override void UpdateProjectionMatrix()
        {
            // The view box scales with Zoom
            float width = Zoom * AspectRatio;
            float height = Zoom;

            ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(
                -width, width,
                -height, height,
                NearPlane, FarPlane);
        }

        public override void HandleMouseMove(Vector2 delta, MouseState mouse)
        {
            // Right-click to Pan
            if (mouse.IsButtonDown(MouseButton.Right))
            {
                // We scale panning by Zoom so it feels consistent at any scale
                Position.X -= delta.X * PanSensitivity * Zoom;
                Position.Y += delta.Y * PanSensitivity * Zoom;
            }
        }

        public override void HandleMouseWheel(float offset)
        {
            Zoom -= offset * ZoomSensitivity;
            if (Zoom < 0.01f) Zoom = 0.01f;
        }

        public override void Update()
        {
            // View is looking at the XY plane at Z=0
            ViewMatrix = Matrix4.LookAt(
                new Vector3(Position.X, Position.Y, 1.0f),
                new Vector3(Position.X, Position.Y, 0.0f),
                Vector3.UnitY);

            UpdateProjectionMatrix();
        }
    }
}