using OpenTK.Mathematics;

namespace OpenTkRenderer
{
    public class ColorMaterial : Material
    {
        public Vector4 Color { get; set; }

        public ColorMaterial(Shader shader, Vector4 color) : base(shader)
        {
            Color = color;
        }

        public override void Apply(Matrix4 model, Camera camera)
        {
            // First, let the base class set the MVP matrices
            base.Apply(model, camera);

            // Now, set our specific color uniform
            // We'll use a helper method in the Shader class
            _shader.SetVector4("uColor", Color);
        }
    }
}