using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NormalUncertainty
{
    public static class VectorExtensions
    {
        public static System.Numerics.Vector2 ToSystem(this OpenTK.Mathematics.Vector2 v)
        {
            return new System.Numerics.Vector2(v.X, v.Y);
        }

        public static OpenTK.Mathematics.Vector2 ToOpenTK(this System.Numerics.Vector2 v)
        {
            return new OpenTK.Mathematics.Vector2(v.X, v.Y);
        }
    }
}
