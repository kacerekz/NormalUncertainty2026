using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

namespace OpenTkRenderer
{
    public class Scene
    {
        public List<GameObject> Objects { get; set; } = new List<GameObject>();
        public CameraManager CameraManager { get; set; }

        // Environment Settings
        public Color4 ClearColor { get; set; } = new Color4(0.1f, 0.1f, 0.1f, 1.0f);
        public bool UseDepthTest { get; set; } = true;

        public Scene(CameraManager cameraManager)
        {
            CameraManager = cameraManager;
        }

        public void Add(GameObject obj) => Objects.Add(obj);

        public void Render()
        {
            // 1. Prepare the state based on scene preferences
            GL.ClearColor(ClearColor);
            if (UseDepthTest) GL.Enable(EnableCap.DepthTest);
            else GL.Disable(EnableCap.DepthTest);

            if (CameraManager?.ActiveCamera == null) return;

            // 2. Render objects
            foreach (var obj in Objects)
            {
                obj.Render(CameraManager.ActiveCamera);
            }
        }
    }
}