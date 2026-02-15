using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NormalUncertainty.Experiments.Convergence._2D
{
    public interface ISamplingStrategy2D
    {
        // Generates up to 'count' samples. Returns the actual number generated.
        int Sample(int count);

        // Returns the current average normal of all samples taken.
        Vector2 GetAverageNormal();

        // Access to the full history for analysis.
        List<Vector2> NormalHistory { get; }
    }
}
