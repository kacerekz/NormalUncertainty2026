using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpanCompression.Structures
{
    public class Face(int i1, int i2, int i3)
    {
        public int i1 = i1;
        public int i2 = i2;
        public int i3 = i3;

        public int[] Indices => [i1, i2, i3];
    }
}
