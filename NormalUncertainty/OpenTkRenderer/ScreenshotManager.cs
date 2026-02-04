using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace OpenTkRenderer
{
    public static class ScreenshotManager
    {
        private static bool _capturePending = false;

        // Call this from OnUpdateFrame
        public static void RequestScreenshot()
        {
            _capturePending = true;
        }

        // Call this from OnRenderFrame (before SwapBuffers)
        public static void ProcessCapture(int width, int height)
        {
            if (!_capturePending) return;

            // Create directory if it doesn't exist
            string folder = "Screenshots";
            Directory.CreateDirectory(folder);

            // Generate unique filename: screenshot_20260204_170005.png
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filePath = Path.Combine(folder, $"screenshot_{timestamp}.png");

            Capture(width, height, filePath);

            _capturePending = false;
            Console.WriteLine($"[Saved] {filePath}");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Don't care.")]
        private static void Capture(int width, int height, string filePath)
        {
            using Bitmap bmp = new(width, height, PixelFormat.Format32bppArgb);
            
            BitmapData data = bmp.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb
            );

            // Use OpenTK's GL.ReadPixels
            GL.ReadPixels(0, 0, width, height, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            bmp.UnlockBits(data);
            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
            bmp.Save(filePath, ImageFormat.Png);
        }
    }
}
