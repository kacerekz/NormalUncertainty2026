using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;

namespace OpenTkRenderer
{
    public class OrbitCamera : Camera
    {
        public Vector3 Target = Vector3.Zero;
        public float Distance = 5.0f;
        public float Pitch = 0.0f;
        public float Yaw = MathHelper.PiOver2;

        // Specialized Settings
        public float Fov = MathHelper.PiOver4;
        public float MouseSensitivity = 0.005f;
        public float ScrollSensitivity = 0.5f;

        public OrbitCamera(float aspectRatio) : base(aspectRatio) { }

        protected override void UpdateProjectionMatrix()
        {
            ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView(Fov, AspectRatio, NearPlane, FarPlane);
        }

        public override void HandleMouseMove(Vector2 delta, MouseState mouse)
        {
            if (mouse.IsButtonDown(MouseButton.Left))
            {
                Yaw += delta.X * MouseSensitivity;
                Pitch -= delta.Y * MouseSensitivity;
                Pitch = MathHelper.Clamp(Pitch, -MathHelper.PiOver2 + 0.01f, MathHelper.PiOver2 - 0.01f);
            }
        }

        public override void HandleMouseWheel(float offset)
        {
            Distance -= offset * ScrollSensitivity;
            if (Distance < 0.1f) Distance = 0.1f;
        }

        public override void Update()
        {
            Vector3 position;
            position.X = Target.X + Distance * MathF.Cos(Pitch) * MathF.Cos(Yaw);
            position.Y = Target.Y + Distance * MathF.Sin(Pitch);
            position.Z = Target.Z + Distance * MathF.Cos(Pitch) * MathF.Sin(Yaw);

            ViewMatrix = Matrix4.LookAt(position, Target, Vector3.UnitY);
            UpdateProjectionMatrix();
        }
    }
}