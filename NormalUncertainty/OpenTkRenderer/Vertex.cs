using OpenTK.Mathematics;
using System.Runtime.InteropServices;

namespace OpenTkRenderer
{
    // StructLayout ensures the data is packed exactly as we expect in memory
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vector3 Position;
        public Vector3 Color;
        public Vector2 TexCoord;

        public Vertex(Vector3 position, Vector3 color, Vector2 texCoord)
        {
            Position = position;
            Color = color;
            TexCoord = texCoord;
        }
    }
}