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
        public CameraManager _cameraManager;
        public Scene _activeScene;

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
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.Disable(EnableCap.DepthTest);

            _activeScene.Render(_cameraManager.ActiveCamera);

            GL.Enable(EnableCap.DepthTest);

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
            //shader.Dispose();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            _cameraManager.OnMouseWheel(e);
        }
    }
}
