using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyLibrary
{
    public class Face
    {
        public int V1;
        public int V2;
        public int V3;

        public Face(int i1, int i2, int i3)
        {
            V1 = i1;
            V2 = i2;
            V3 = i3;
        }

        public int[] Indices => [V1, V2, V3];

        public override bool Equals(object obj)
        {
            if (obj is Face other)
            {
                if (((this.V1 == other.V1) || (this.V1 == other.V2) || (this.V1 == other.V3)) &&
                    ((this.V2 == other.V1) || (this.V2 == other.V2) || (this.V2 == other.V3)) &&
                    ((this.V3 == other.V1) || (this.V3 == other.V2) || (this.V3 == other.V3)))
                    return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(V1, V2, V3);
        }
    }
}
