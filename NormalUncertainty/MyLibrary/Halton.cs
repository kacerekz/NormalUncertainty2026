using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyLibrary
{
    public static class Halton
    {
        /// <summary>
        /// Calculates the Halton sequence value for a given index and base.
        /// </summary>
        public static float Get(int index, int baseVal)
        {
            float result = 0f;
            float f = 1f / baseVal;
            int i = index;

            while (i > 0)
            {
                result += f * (i % baseVal);
                i = (int)(i / baseVal);
                f /= baseVal;
            }

            return result;
        }
    }
}
