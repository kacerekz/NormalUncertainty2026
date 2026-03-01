using SpanCompression.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SpanCompression
{
    public class NormalUncertainty
    {
        public static Dictionary<int, double> Calculate(Face[] faces, Dictionary<int, Vector3[]> samples)
        {
            Dictionary<int, double> sum = [];
            double[] stdev = new double[faces.Length];

            foreach (var sample in samples)
                sum[sample.Key] = 0.0;

            //for (int i = 0; i < faces.Length; i++)
            Parallel.For(0, faces.Length, i =>
            {
                Face f = faces[i];
                Vector3[] pts1 = samples[f.i1];
                Vector3[] pts2 = samples[f.i2];
                Vector3[] pts3 = samples[f.i3];
                stdev[i] = GetTriangleNormalUncertainty(pts1, pts2, pts3);
            });

            for (int i = 0; i < faces.Length; i++)
            {
                Face f = faces[i];
                sum[f.i1] += stdev[i];
                sum[f.i2] += stdev[i];
                sum[f.i3] += stdev[i];
            }

            return sum;
        }

        public static double GetTriangleNormalUncertainty(Vector3[] pts1, Vector3[] pts2, Vector3[] pts3)
        {
            Vector3 sumNormal = Vector3.Zero;
            int count = 0;

            for (int v1 = 0; v1 < pts1.Length; v1++)
            {
                for (int v2 = 0; v2 < pts2.Length; v2++)
                {
                    for (int v3 = 0; v3 < pts3.Length; v3++)
                    {
                        var tn = MathUtils.GetTriangleNormal(pts1[v1], pts2[v2], pts3[v3]);

                        if (!float.IsNaN(tn.X))
                        {
                            sumNormal += tn;
                            count++;
                        }
                    }
                }
            }

            if (count == 0) return 0.0;

            Vector3 avgNormal = Vector3.Normalize(sumNormal);
            double stdev = 0.0;

            for (int v1 = 0; v1 < pts1.Length; v1++)
            {
                for (int v2 = 0; v2 < pts2.Length; v2++)
                {
                    for (int v3 = 0; v3 < pts3.Length; v3++)
                    {
                        var tn = MathUtils.GetTriangleNormal(pts1[v1], pts2[v2], pts3[v3]);

                        if (!float.IsNaN(tn.X))
                        {
                            var angle = MathUtils.GetAngleBetween(avgNormal, tn);
                            stdev += angle * angle;
                        }
                    }
                }
            }

            return Math.Sqrt(stdev / count);
        }

        //public static double GetTriangleNormalUncertainty(Vector3[] pts1, Vector3[] pts2, Vector3[] pts3)
        //{
        //    List<Vector3> normals = [];
        //    double stdev = 0.0;

        //    for (int v1 = 0; v1 < pts1.Length; v1++)
        //    {
        //        for (int v2 = 0; v2 < pts2.Length; v2++)
        //        {
        //            for (int v3 = 0; v3 < pts3.Length; v3++)
        //            {
        //                var tn = MathUtils.GetTriangleNormal(
        //                    pts1[v1], pts2[v2], pts3[v3]);
        //                normals.Add(tn);
        //            }

        //            var avg = GetAverageNormal(normals);

        //            foreach (var normal in normals)
        //            {
        //                var angle = MathUtils.GetAngleBetween(avg, normal);
        //                stdev += angle * angle;
        //            }

        //            stdev /= normals.Count;
        //            stdev = Math.Sqrt(stdev);
        //        }
        //    }

        //    return stdev;
        //}

        private static Vector3 GetAverageNormal(List<Vector3> normals)
        {
            Vector3 sum = Vector3.Zero;
            foreach (var n in normals)
                sum += n;
            return Vector3.Normalize(sum);
        }
    }
}
