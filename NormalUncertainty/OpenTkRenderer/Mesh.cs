using OpenTK.Graphics.OpenGL4;
using System;
using System.Runtime.InteropServices;

namespace OpenTkRenderer
{
    public class Mesh : IDisposable
    {
        private int _vao;
        private int _vbo;
        private int _vertexCount;

        private PrimitiveType _type;

        public Mesh(Vertex[] vertices, PrimitiveType type)
        {
            _type = type; 
            _vertexCount = vertices.Length;

            // 1. Generate and bind the VAO
            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);

            // 2. Generate and fill the VBO
            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * Marshal.SizeOf<Vertex>(), vertices, BufferUsageHint.StaticDraw);

            // 3. Define the layout for the shader (matching test.vert)
            // Position (Location 0)
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Marshal.SizeOf<Vertex>(), 0);

            // Color (Location 1) - We'll add this to the shader next!
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, Marshal.SizeOf<Vertex>(), Marshal.OffsetOf<Vertex>("Color"));

            // TexCoord (Location 2)
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, Marshal.SizeOf<Vertex>(), Marshal.OffsetOf<Vertex>("TexCoord"));

            GL.BindVertexArray(0);
        }

        public void Draw()
        {
            GL.BindVertexArray(_vao);
            GL.DrawArrays(_type, 0, _vertexCount);
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_vbo);
        }
    }
}