using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpanCompression.Structures
{
    public class Span(Axis axis, Cell parent, float min, float max)
    {
        public Axis axis = axis;
        public Cell parent = parent;
        public float Min { get; set; } = min;
        public float Max { get; set; } = max;
        public float Center => (Min + Max) * .5f;
        public float Size => Max - Min;
        
        public void Split(int bit)
        {
            if (bit == 0)
                Max = Center;
            else
                Min = Center;
        }

        public int Split(float trueValue)
        {
            int bit;

            if (trueValue <= Center)
            {
                bit = 0;
                Max = Center;
            }
            else
            {
                bit = 1;
                Min = Center;
            }
            return bit;
        }

        public float GetRandomPoint()
        {
            return (float)(Globals.Random.NextDouble() * Size + Min);
        }
    }
}
