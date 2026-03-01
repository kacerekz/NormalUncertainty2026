using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpanCompression.Structures
{
    public class Edge
    {
        public int v1;
        public int v2;

        public Edge(int v1, int v2)
        {
            this.v1 = v1;
            this.v2 = v2;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not Edge)
                return false;

            var oe = (Edge)obj;
            return v1 == oe.v1
                && v2 == oe.v2;
        }

        public override int GetHashCode()
        {
            return v1 * 1000 + v2;
        }
    }
}
