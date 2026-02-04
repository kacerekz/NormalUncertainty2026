using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace OpenTkRenderer
{
    public class CameraManager
    {
        public Camera ActiveCamera { get; set; }
        private Vector2 _lastMousePos;
        private bool _firstMove = true;

        public CameraManager(Camera initialCamera) => ActiveCamera = initialCamera;

        public void Update(KeyboardState keyboard, MouseState mouse, double deltaTime)
        {
            if (ActiveCamera == null) return;

            Vector2 currentMousePos = new Vector2(mouse.X, mouse.Y);
            if (_firstMove)
            {
                _lastMousePos = currentMousePos;
                _firstMove = false;
            }

            Vector2 delta = currentMousePos - _lastMousePos;
            _lastMousePos = currentMousePos;

            // Delegate to the camera
            ActiveCamera.HandleInput(keyboard, deltaTime);
            ActiveCamera.HandleMouseMove(delta, mouse);
            ActiveCamera.Update();
        }

        public void OnMouseWheel(MouseWheelEventArgs e) => ActiveCamera?.HandleMouseWheel(e.OffsetY);
        public void OnResize(int w, int h) => ActiveCamera?.UpdateAspectRatio(w / (float)h);
    }
}