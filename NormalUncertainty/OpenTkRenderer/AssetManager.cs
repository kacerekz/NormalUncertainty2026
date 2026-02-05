namespace OpenTkRenderer
{
    public static class AssetManager
    {
        private static readonly Dictionary<string, Shader> _shaders = new();
        private static readonly Dictionary<string, Mesh> _meshes = new();
        private static readonly Dictionary<string, Material> _materials = new();

        public static void AddShader(string name, Shader shader) => _shaders[name] = shader;
        public static Shader GetShader(string name) => _shaders[name];

        public static void AddMesh(string name, Mesh mesh) => _meshes[name] = mesh;
        public static Mesh GetMesh(string name) => _meshes[name];

        public static void AddMaterial(string name, Material material) => _materials[name] = material;
        public static Material GetMaterial(string name) => _materials[name];

        public static void DisposeAll()
        {
            foreach (var mesh in _meshes.Values) mesh.Dispose();
            foreach (var shader in _shaders.Values) shader.Dispose();
            _meshes.Clear();
            _shaders.Clear();
            _materials.Clear();
        }
    }
}