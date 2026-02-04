using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenTkRenderer
{
    public class Game : GameWindow
    {
        float[] vertices = {
            -0.5f, -0.5f, 0.0f, //Bottom-left vertex
             0.5f, -0.5f, 0.0f, //Bottom-right vertex
             0.0f,  0.5f, 0.0f  //Top vertex
        };

        int VertexBufferObject;
        int VertexArrayObject;

        Shader shader;

        private CameraManager _cameraManager;
        
        public Game(int width, int height, string title)
            : base(GameWindowSettings.Default, new()
            {
                ClientSize = (width, height),
                Title = title
            })
        { }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            GL.Enable(EnableCap.DepthTest);

            // Initialize with current window aspect ratio
            
            var orthoCam = new OrthoCamera(Size.X / (float)Size.Y);
            orthoCam.Zoom = 2.0f; // Zoom out a bit to see the whole triangle
            orthoCam.Position = new Vector2(0, 0);

            var orbitCam = new OrbitCamera(Size.X / (float)Size.Y);

            _cameraManager = new CameraManager(orbitCam);

            VertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            shader = new Shader("Shaders/test.vert", "Shaders/test.frag");

            VertexArrayObject = GL.GenVertexArray();

            // ..:: Initialization code (done once (unless your object frequently changes)) :: ..
            // 1. bind Vertex Array Object
            GL.BindVertexArray(VertexArrayObject);
            // 2. copy our vertices array in a buffer for OpenGL to use
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            // 3. then set our vertex attributes pointers
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (!IsFocused) return;

            // Pass input to the manager
            _cameraManager.Update(KeyboardState, MouseState, args.Time);

            if (KeyboardState.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            if (KeyboardState.IsKeyPressed(Keys.F12))
            {
                ScreenshotManager.RequestScreenshot();
            }
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit); // Clear depth too! [cite: 471]

            shader.Use();

            // 1. Get uniform locations from the shader program [cite: 565]
            // Note: In a production app, you'd cache these locations for speed.
            int modelLoc = GL.GetUniformLocation(shader.Handle, "model");
            int viewLoc = GL.GetUniformLocation(shader.Handle, "view");
            int projLoc = GL.GetUniformLocation(shader.Handle, "projection");

            // 2. Prepare the matrices
            Matrix4 model = Matrix4.Identity; // The triangle stays at the origin
            Matrix4 view = _cameraManager.ActiveCamera.ViewMatrix;
            Matrix4 projection = _cameraManager.ActiveCamera.ProjectionMatrix;

            // 3. Send them to the GPU [cite: 84]
            // The 'false' argument means we are NOT transposing them (OpenTK matrices are already in the format GL expects)
            GL.UniformMatrix4(modelLoc, false, ref model);
            GL.UniformMatrix4(viewLoc, false, ref view);
            GL.UniformMatrix4(projLoc, false, ref projection);

            GL.BindVertexArray(VertexArrayObject);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            ScreenshotManager.ProcessCapture(Size.X, Size.Y);
            SwapBuffers();
        }

        protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
        {
            base.OnFramebufferResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
            _cameraManager.OnResize(e.Width, e.Height);
        }

        protected override void OnUnload()
        {
            base.OnUnload();
            shader.Dispose();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            _cameraManager.OnMouseWheel(e);
        }
    }
}
