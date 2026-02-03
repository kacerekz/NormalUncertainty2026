
using MyLibrary;
using System.Numerics;

namespace NormalUncertainty
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Vector2 reference;
            Vector2 current;
            
            float step = 10f;
            float shift = 90f;

            reference = new Vector2(0f, 1f);

            for (float angle = 0; angle < 360; angle += step)
            {
                float angleR = MathUtil.ToRadians(angle + shift);

                current = new Vector2(
                    MathF.Cos(angleR), 
                    MathF.Sin(angleR));

                float differenceR = AcosDifference(reference, current);
                float difference = MathUtil.ToDegrees(differenceR);  
                difference = MathF.Round(difference, 0);
                Console.WriteLine(difference);
            }
        }




    }
}
