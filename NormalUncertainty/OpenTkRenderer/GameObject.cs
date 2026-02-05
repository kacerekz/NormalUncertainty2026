using OpenTK.Mathematics;

namespace OpenTkRenderer
{
    public class GameObject
    {
        public Mesh Mesh { get; set; }
        public Material Material { get; set; }
        public Matrix4 Transform { get; set; } = Matrix4.Identity;

        public GameObject(Mesh mesh, Material material)
        {
            Mesh = mesh;
            Material = material;
        }

        public void Render(Camera camera)
        {
            Material.Use();
            Material.Apply(Transform, camera);
            Mesh.Draw();
        }
    }
}