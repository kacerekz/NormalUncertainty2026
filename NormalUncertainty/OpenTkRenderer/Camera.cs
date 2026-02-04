using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace OpenTkRenderer
{
    public abstract class Camera
    {
        public Matrix4 ViewMatrix { get; protected set; }
        public Matrix4 ProjectionMatrix { get; protected set; }

        protected float AspectRatio;
        public float NearPlane = 0.1f;
        public float FarPlane = 100f;

        public Camera(float aspectRatio)
        {
            AspectRatio = aspectRatio;
        }

        public void UpdateAspectRatio(float aspectRatio)
        {
            AspectRatio = aspectRatio;
            UpdateProjectionMatrix();
        }

        protected abstract void UpdateProjectionMatrix();
        public abstract void Update();

        // Input Hooks
        public virtual void HandleMouseMove(Vector2 delta, MouseState mouse) { }
        public virtual void HandleMouseWheel(float offset) { }
        public virtual void HandleInput(KeyboardState keyboard, double deltaTime) { }
    }
}