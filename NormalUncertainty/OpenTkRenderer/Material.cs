using OpenTK.Mathematics;

namespace OpenTkRenderer
{
    public class Material
    {
        protected Shader _shader;

        public Material(Shader shader)
        {
            _shader = shader;
        }

        public virtual void Use()
        {
            _shader.Use();
        }

        public virtual void Apply(Matrix4 model, Camera camera)
        {
            _shader.SetMatrix4("model", model);
            _shader.SetMatrix4("view", camera.ViewMatrix);
            _shader.SetMatrix4("projection", camera.ProjectionMatrix);
        }
    }
}