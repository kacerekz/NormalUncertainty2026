using OpenTkRenderer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NormalUncertainty.Experiments
{
    public abstract class Experiment
    {
        protected Scene Scene;
        public int Width { get; set; }
        public int Height { get; set; }

        public void Run(string title = "Experiment")
        {
            using (Game game = new Game(Width, Height, title))
            {
                Initialize(Width, Height); // Setup meshes/materials via AssetManager
                game._activeScene = Scene;
                game.Run();
                AssetManager.DisposeAll(); // Auto-cleanup on exit
            }
        }

        // Setup resources and build the scene
        public abstract void Initialize(int width, int height);

        // Handle logic (adding samples, etc.)
        public virtual void Update(double deltaTime) { }
    }
}