using System.Collections.Generic;
using System.Numerics;

namespace NormalUncertainty.Experiments.Convergence._3D
{
    public interface ISamplingStrategy3D
    {
        int Sample(int count);
        Vector3 GetAverageNormal();
        List<Vector3> NormalHistory { get; }
    }
}