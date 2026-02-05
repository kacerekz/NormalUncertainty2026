using System.Collections.Generic;

namespace OpenTkRenderer
{
    public class Scene
    {
        // Using a list ensures we preserve the Draw Order (Painter's Algorithm)
        public List<GameObject> Objects { get; set; } = new List<GameObject>();

        public void Add(GameObject obj) => Objects.Add(obj);

        public void Render(Camera camera)
        {
            foreach (var obj in Objects)
            {
                obj.Render(camera);
            }
        }
    }
}