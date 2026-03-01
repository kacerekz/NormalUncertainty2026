using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SpanCompression._3rdParty
{
    interface IBinaryStats
    {
        uint P0 { get; }
        uint P1 { get; }
        void UpdateBit(bool val);
        void UpdateValue(int val);
    }

    /// <summary>
    /// Optimised arithmetic encoder. Tests several configurations and selects the best cutoff, order, context count and context depth.
    /// </summary>
    public class SuperArithCoder
    {        
        /// <summary>
        /// Encodes the data into a stream.
        /// </summary>
        /// <param name="data">Data to encode</param>
        /// <param name="target">Target stream to write to</param>
        public static void Encode(int[] data, Stream target)
        {
            int[] best = new int[4];
            int lowest = int.MaxValue;
            for (int cutoff = 1;cutoff<20;cutoff++)
                for (int order = 1; order <10;order++)
                    for (int count = 1;count < 20;count++)
                        for (int depth = 0; depth < 4; depth++)
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                ArithCoder coder = new ArithCoder(ms, cutoff, order, count, depth);
                                coder.Encode(data);
                                if (ms.Position < lowest)
                                {
                                    lowest = (int)ms.Position;
                                    best[0] = cutoff;
                                    best[1] = order;
                                    best[2] = count;
                                    best[3] = depth;
                                }
                            }
                        }
            int code = best[0] + best[1] * 100 + best[2] * 10000 + best[3] * 1000000;
            BinaryWriter bw = new BinaryWriter(target);
            bw.Write(code);
            ArithCoder c = new ArithCoder(target, best[0], best[1], best[2], best[3]);
            c.Encode(data);            
        }

        /// <summary>
        /// Decodes the data from a stream previously saved by the superarithcoder
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static int[] Decode(Stream source)
        {
            BinaryReader br = new BinaryReader(source);
            int code = br.ReadInt32();
            int cutoff = code % 100;
            int order = (code / 100) % 100;
            int count = (code / 10000) % 100;
            int depth = (code / 1000000);
            ArithCoder decoder = new ArithCoder(source, cutoff, order, count, depth);
            return (decoder.Decode());
        }
    }

    /// <summary>
    /// Class encapsulating an arithmetic coder
    /// </summary>
    public class ArithCoder
    {        
        Stream target;

        /// <summary>
        /// constructs a new instance of coder, bound to a stream
        /// </summary>
        /// <param name="s">stram to bind the coder</param>
        public ArithCoder(Stream s)
        {
            this.target = s;
        }

        /// <summary>
        /// cosntructs a new instance of the coder, bound to a stream
        /// </summary>
        /// <param name="s">stream to bind to the coder</param>
        /// <param name="cutoff">cut off value for the binarizer</param>
        /// <param name="order">order of the binarizer</param>
        /// <param name="count">number of contexts</param>
        /// <param name="depth">depth of contexts</param>
        public ArithCoder(Stream s, int cutoff, int order, int count, int depth)
        {
            this.target = s;
            this.contextDepth = depth;
            this.contextCount = count;
            this.binarizerCutoff = cutoff;
            this.binarizerOrder = order;
        }

        int contextDepth = 2;
        int binarizerOrder = 3;
        int binarizerCutoff = 9;
        int contextCount = 12;        

        /// <summary>
        /// encodes the scalar data into a stream
        /// </summary>
        /// <param name="data">data to encode</param>
        /// <param name="q">quantization constant to be used</param>
        public void Encode(double[] data, double q)
        {
            int[] iData = new int[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                iData[i] = (int)Math.Round(data[i] / q);
            }
            Encode(iData);
        }

        /// <summary>
        /// decodes doubles previously encoded using quantization
        /// </summary>
        /// <param name="q">quantization constant</param>
        /// <returns>decoded data</returns>
        public double[] DecodeDoubles(double q)
        {
            int[] data = Decode();
            double[] result = new double[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = data[i] * q;
            }
            return (result);
        }

        /// <summary>
        /// encodes an array of integers into the associated stream
        /// </summary>
        /// <param name="values">values to encode</param>
        public void Encode(int[] values)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Coder coder = new Coder(ms);

                bool[][] bins = binarize(values, binarizerCutoff, binarizerOrder);
                IBinaryStats[] stats = new IBinaryStats[contextCount];
                for (int i = 0; i < stats.Length; i++)
                    stats[i] = new ContextBinaryStats(contextDepth);
                IBinaryStats rest = new ContextBinaryStats(contextDepth);
                IBinaryStats sgn = new ContextBinaryStats(contextDepth);
                for (int i = 0; i < bins.Length; i++)
                {
                    for (int j = 0; j < bins[i].Length; j++)
                    {
                        IBinaryStats bs = j < stats.Length ? stats[j] : rest;
                        if ((j > 0) && (j == bins[i].Length - 1))
                            bs = sgn;
                        coder.encodeBit(bs.P0, bs.P1, bins[i][j]);
                        bs.UpdateBit(bins[i][j]);
                    }
                }

                coder.finishEncode();

                int encodedLength = (int)ms.Length;
                BinaryWriter bw = new BinaryWriter(target);
                bw.Write(values.Length);
                bw.Write(encodedLength);
                ms.Position = 0;
                byte[] buffer = new byte[encodedLength];
                ms.Read(buffer, 0, encodedLength);
                target.Write(buffer, 0, encodedLength);
            }
        }

        /// <summary>
        /// encodes an array of longs into the associated stream
        /// </summary>
        /// <param name="values">values to encode</param>
        public void Encode(long[] values)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Coder coder = new Coder(ms);

                bool[][] bins = binarize(values, binarizerCutoff, binarizerOrder);
                IBinaryStats[] stats = new IBinaryStats[contextCount];
                for (int i = 0; i < stats.Length; i++)
                    stats[i] = new ContextBinaryStats(contextDepth);
                IBinaryStats rest = new ContextBinaryStats(contextDepth);
                IBinaryStats sgn = new ContextBinaryStats(contextDepth);
                for (int i = 0; i < bins.Length; i++)
                {
                    for (int j = 0; j < bins[i].Length; j++)
                    {
                        IBinaryStats bs = j < stats.Length ? stats[j] : rest;
                        if ((j > 0) && (j == bins[i].Length - 1))
                            bs = sgn;
                        coder.encodeBit(bs.P0, bs.P1, bins[i][j]);
                        bs.UpdateBit(bins[i][j]);
                    }
                }

                coder.finishEncode();

                int encodedLength = (int)ms.Length;
                BinaryWriter bw = new BinaryWriter(target);
                bw.Write(values.Length);
                bw.Write(encodedLength);
                ms.Position = 0;
                byte[] buffer = new byte[encodedLength];
                ms.Read(buffer, 0, encodedLength);
                target.Write(buffer, 0, encodedLength);
            }
        }

        public bool useDirectCoding = false;
        public int directCodeLength = 3;

        /// <summary>
        /// encodes an array of integers into the associated stream
        /// </summary>
        /// <param name="values"></param>
        public void EncodeUnsigned(int[] values, int count)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Coder coder = new Coder(ms);

                bool[][] bins;
                if (useDirectCoding)
                    bins = binarizeDirect(values, directCodeLength);
                else
                    bins = binarizeUnsigned(values, binarizerCutoff, binarizerOrder, count);
                IBinaryStats[] stats = new IBinaryStats[contextCount];
                for (int i = 0; i < stats.Length; i++)
                    stats[i] = new ContextBinaryStats(contextDepth);
                IBinaryStats rest = new ContextBinaryStats(contextDepth);
                for (int i = 0; i < bins.Length; i++)
                {
                    for (int j = 0; j < bins[i].Length; j++)
                    {
                        IBinaryStats bs = j < stats.Length ? stats[j] : rest;
                        coder.encodeBit(bs.P0, bs.P1, bins[i][j]);
                        bs.UpdateBit(bins[i][j]);
                    }
                    foreach (IBinaryStats st in stats)
                        st.UpdateValue(values[i]);
                    rest.UpdateValue(values[i]);
                }
                coder.finishEncode();

                int encodedLength = (int)ms.Length;
                BinaryWriter bw = new BinaryWriter(target);
                bw.Write(count);
                bw.Write(encodedLength);
                ms.Position = 0;
                byte[] buffer = new byte[encodedLength];
                ms.Read(buffer, 0, encodedLength);
                target.Write(buffer, 0, encodedLength);
            }
        }

        private bool[][] binarizeDirect(int[] values, int directCodeLength)
        {
            bool[][] result = new bool[values.Length][];
            for (int i = 0; i < values.Length; i++)
            {
                result[i] = new bool[directCodeLength];
                int val = values[i];
                for (int j = 0; j < directCodeLength; j++)
                {
                    result[i][directCodeLength - j - 1] = (val % 2 == 1);
                    val = val / 2;
                }
            }
            return (result);
        }

        /// <summary>
        /// encodes an array of booleans into the associated stream
        /// </summary>
        /// <param name="values"></param>
        public void Encode(bool[] values)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Coder coder = new Coder(ms);

                IBinaryStats stats = new ContextBinaryStats(contextDepth);
                for (int i = 0; i < values.Length; i++)
                {
                    coder.encodeBit(stats.P0, stats.P1, values[i]);
                    stats.UpdateBit(values[i]);
                }
                coder.finishEncode();

                int encodedLength = (int)ms.Length;
                BinaryWriter bw = new BinaryWriter(target);
                bw.Write(values.Length);
                bw.Write(encodedLength);
                ms.Position = 0;
                byte[] buffer = new byte[encodedLength];
                ms.Read(buffer, 0, encodedLength);
                target.Write(buffer, 0, encodedLength);
            }
        }
        
        /// <summary>
        /// static method for data encoding
        /// </summary>
        /// <param name="values">integer values to be encoded</param>
        /// <param name="target">stredm for data output</param>
        public static void encode(int[] values, Stream target)
        {
            ArithCoder coder = new ArithCoder(target);
            coder.Encode(values);
        }

        /// <summary>
        /// static method for data encoding
        /// </summary>
        /// <param name="values">long values to be encoded</param>
        /// <param name="target">stream for data output</param>
        public static void encode(long[] values, Stream target)
        {
            ArithCoder coder = new ArithCoder(target);
            coder.Encode(values);
        }

        /// <summary>
        /// static method for data encoding
        /// </summary>
        /// <param name="values">integer values to be encoded</param>
        /// <param name="target">stredm for data output</param>
        public static void encode(bool[] values, Stream target)
        {
            ArithCoder coder = new ArithCoder(target);
            coder.Encode(values);
        }

        /// <summary>
        /// static method for data encoding unsigned data
        /// </summary>
        /// <param name="values">integer values to be encoded</param>
        /// <param name="target">stredm for data output</param>
        public static void encodeUnsigned(int[] values, Stream target)
        {
            ArithCoder coder = new ArithCoder(target);
            coder.EncodeUnsigned(values, values.Length);
        }
        
        /// <summary>
        /// static method for data decoding
        /// </summary>
        /// <param name="source">Source data stream</param>
        /// <returns>reconstructed integer values</returns>
        public static int[] decode(Stream source)
        {
            ArithCoder coder = new ArithCoder(source);
            return (coder.Decode());
        }

        /// <summary>
        /// static method for data decoding
        /// </summary>
        /// <param name="source">Source data stream</param>
        /// <returns>reconstructed integer values</returns>
        public static long[] decodeLongs(Stream source)
        {
            ArithCoder coder = new ArithCoder(source);
            return (coder.DecodeLongs());
        }

        /// <summary>
        /// static method for data decoding unsigned data
        /// </summary>
        /// <param name="source">Source data stream</param>
        /// <returns>reconstructed integer values</returns>
        public static int[] decodeUnsigned(Stream source)
        {
            ArithCoder coder = new ArithCoder(source);
            return (coder.DecodeUnsigned());
        }

        /// <summary>
        /// static method for data decoding
        /// </summary>
        /// <param name="source">Source data stream</param>
        /// <returns>reconstructed integer values</returns>
        public static bool[] decodeBools(Stream source)
        {
            ArithCoder coder = new ArithCoder(source);
            return (coder.DecodeBools());
        }

        private static void egk(int val, List<bool> target, int k)
        {
            while (true)
            {
                if (val >= (1 << k))
                {
                    target.Add(true);
                    val = val - (1 << k);
                    k++;
                }
                else
                {
                    target.Add(false);
                    while ((k--) > 0)
                    {
                        target.Add(((val >> k) & (0x01)) > 0);
                    }
                    break;
                }
            }
        }

        private static void egk(long val, List<bool> target, int k)
        {
            while (true)
            {
                if (val >= (1L << k))
                {
                    target.Add(true);
                    val = val - (1L << k);
                    k++;
                }
                else
                {
                    target.Add(false);
                    while ((k--) > 0)
                    {
                        target.Add(((val >> k) & (0x01)) > 0);
                    }
                    break;
                }
            }
        }

        static int deBinarize(bool[] binary, int cutoff, int k)
        {
            int result = 0;
            if (binary.Length == 1)
                return (0);
            if (binary.Length < (cutoff + 2))
            {
                result = binary.Length - 2;
                if (binary[binary.Length - 1])
                    result = -result;
                return (result);
            }
            int p = 9;
            int lk = k;
            while (binary[p] == true)
            {
                result += 1 << lk;
                lk++;
                p++;
                
            }
            p++;
            int tail = 0;
            for (int i = 0; i < lk; i++)
            {
                tail *= 2;
                if (binary[p])
                    tail++;
                p++;
            }            
            result += cutoff + tail;
            if (binary[p])
            {
                result = -result;
            }
            
            return (result);
        }

        static long deBinarizeLong(bool[] binary, int cutoff, int k)
        {
            long result = 0;
            if (binary.Length == 1)
                return (0);
            if (binary.Length < (cutoff + 2))
            {
                result = binary.Length - 2;
                if (binary[binary.Length - 1])
                    result = -result;
                return (result);
            }
            int p = 9;
            int lk = k;
            while (binary[p] == true)
            {
                result += 1 << lk;
                lk++;
                p++;

            }
            p++;
            int tail = 0;
            for (int i = 0; i < lk; i++)
            {
                tail *= 2;
                if (binary[p])
                    tail++;
                p++;
            }
            result += cutoff + tail;
            if (binary[p])
            {
                result = -result;
            }

            return (result);
        }

        static int deBinarizeUnsigned(bool[] binary, int cutoff, int k)
        {
            int result = 0;
            if (binary.Length == 1)
                return (0);
            if (binary.Length < (cutoff + 1))
            {
                result = binary.Length - 1;                
                return (result);
            }
            int p = 9;
            int lk = k;
            while (binary[p] == true)
            {
                result += 1 << lk;
                lk++;
                p++;

            }
            p++;
            int tail = 0;
            for (int i = 0; i < lk; i++)
            {
                tail *= 2;
                if (binary[p])
                    tail++;
                p++;
            }
            result += cutoff + tail;            

            return (result);
        }

        static bool[] getCode(int val, int cutoff, int k)
        {
            List<bool> result = new List<bool>(15);
            if (val == 0)
            {
                result.Add(false);
            }
            else
            {
                int abs = 0;
                int min = 0;
                try
                {
                    abs = Math.Abs(val);
                    min = Math.Min(cutoff, abs);
                }
                catch(Exception)
                {
                    abs = val;
                    Console.WriteLine("error compressing value "+val+". Possibly too high absolute value");
                    throw;
                }                
                for (int i = 0; i < min; i++)
                {
                    result.Add(true);
                }
                if (abs < cutoff)
                {
                    result.Add(false);
                }
                else
                {
                    int red = abs - cutoff;
                    egk(red, result, k);
                }
                if (val < 0)
                    result.Add(true);
                else
                    result.Add(false);
            }
            return (result.ToArray());
        }

        static bool[] getCode(long val, int cutoff, int k)
        {
            List<bool> result = new List<bool>();
            if (val == 0)
            {
                result.Add(false);
            }
            else
            {
                long abs = 0;
                try
                {
                    abs = Math.Abs(val);
                }
                catch (Exception)
                {
                    abs = val;
                    Console.WriteLine("error compressing value " + val + ". Possibly too high absolute value");
                    throw;
                }
                long min = Math.Min(cutoff, abs);
                for (int i = 0; i < min; i++)
                {
                    result.Add(true);
                }
                if (abs < cutoff)
                {
                    result.Add(false);
                }
                else
                {
                    long red = abs - cutoff;
                    egk(red, result, k);
                }
                if (val < 0)
                    result.Add(true);
                else
                    result.Add(false);
            }
            return (result.ToArray());
        }

        static bool[] getCodeUnsigned(int val, int cutoff, int k)
        {
            List<bool> result = new List<bool>();
            if (val == 0)
            {
                result.Add(false);
            }
            else
            {
                int abs = 0;
                try
                {
                    abs = Math.Abs(val);
                }
                catch (Exception)
                {
                    abs = val;
                    Console.WriteLine("error compressing value " + val + ". Possibly too high absolute value");
                    throw;
                }
                int min = Math.Min(cutoff, abs);
                for (int i = 0; i < min; i++)
                {
                    result.Add(true);
                }
                if (abs < cutoff)
                {
                    result.Add(false);
                }
                else
                {
                    int red = abs - cutoff;
                    egk(red, result, k);
                }                
            }
            return (result.ToArray());
        }

        static bool isCode(List<bool> code, int cutoff, int k, ref int lst)
        {
            if (code.Count == 0)
                return (false);
            else if (code.Count == 1)
            {
                if (code[0] == false)
                    return (true);
                else
                    return (false);
            }
            else
            {
                if ((code.Count-2) < cutoff)
                {
                    if (code[code.Count - 2] == false)
                        return (true);
                    else
                        return (false);
                }
                if (lst<0)
                    return false;
                else
                {
                    int glk = k+lst;
                    if (code.Count == cutoff + (glk - k) + 1 + glk+1)
                        return true;
                    else 
                        return false;
                }
                /*int p = 0;
                while ((p<code.Count)&&(code[p] == true)&&(p<cutoff))
                {
                    p++;
                }
                if (p < cutoff)
                {
                    if (code.Count == p + 2)
                        return (true);
                    else
                        return (false);
                }*/
                /*else
                {
                    int p = cutoff;
                    int lk = k;
                    while ((p < code.Count)&&(code[p] == true))
                    {
                        p++;
                        lk++;
                    }
                    if (p == code.Count)
                        return (false);
                    if (code.Count == cutoff + (lk - k) + 1 + lk + 1)
                        return (true);
                    else
                        return (false);
                }*/
            }
            //return (false);
        }

        static bool isCodeUnsigned(List<bool> code, int cutoff, int k, ref int lst)
        {
            if (code.Count == 0)
                return (false);
            else if (code.Count == 1)
            {
                if (code[0] == false)
                    return (true);
                else
                    return (false);
            }
            else
            {
                if ((code.Count - 1) < cutoff)
                {
                    if (code[code.Count - 1] == false)
                        return (true);
                    else
                        return (false);
                }
                if (lst < 0)
                {
                    if (code[code.Count-1]==true)
                        return false;
                    else
                    {
                        lst = code.Count-cutoff-1;
                        return(false);
                    }
                }                    
                else
                {
                    int glk = k + lst;
                    if (code.Count == cutoff + (glk - k) + 1 + glk)
                        return true;
                    else
                        return false;
                }
            }
            /*
            if (code.Count == 0)
                return (false);
            else if (code.Count == 1)
            {
                if (code[0] == false)
                    return (true);
                else
                    return (false);
            }
            else
            {                
                int p = 0;
                while ((p < code.Count) && (code[p] == true) && (p < cutoff))
                {
                    p++;
                }
                if (p < cutoff)
                {
                    if (code.Count == p + 1)
                        return (true);
                    else
                        return (false);
                }
                else
                {
                    int lk = k;
                    while ((p < code.Count) && (code[p] == true))
                    {
                        p++;
                        lk++;
                    }
                    if (p == code.Count)
                        return (false);
                    if (code.Count == cutoff + (lk - k) + 1 + lk)
                        return (true);
                    else
                        return (false);
                }
            }
            //return (false);*/
        }

        static bool isSgn(List<bool> code, int cutoff, int k, ref int lst)
        {
            if (code.Count == 0)
                return (false);
            else if (code.Count == 1)
            {                
                return (false);
            }
            else if ((code.Count-1)<cutoff)
            {
                if (code[code.Count - 1] == false)
                    return (true);
                else
                    return (false);
            }
                
            else
            {
                //bool guess = false;
                if (lst<0)
                {
                    if (code[code.Count-1]==false)
                        lst = code.Count - cutoff - 1;
                    return false;
                    //guess = false;
                }
                else
                {
                    int glk = k+lst;
                    if (code.Count == cutoff + (glk - k) + 1 + glk)
                        return true;
                        //guess = true;
                    else
                        return false;
                        //guess = false;
                }
                /*int p = cutoff;
                
                int lk = k;
                while ((p < code.Count) && (code[p] == true))
                {
                    p++;
                    lk++;
                }
                if (p == code.Count)
                {
                    if (guess != false)
                        Console.WriteLine("Error");
                    return (false);
                }
                if (code.Count == cutoff + (lk - k) + 1 + lk)
                {
                    if (guess != true)
                        Console.WriteLine("Error");
                    return (true);
                }
                else
                {
                    if (guess != false)
                        Console.WriteLine("Error");
                    return (false);
                }*/
            }
            //return (false);
        }


        private bool[][] binarize(int[] values, int cutoff, int order)
        {
            bool[][] result = new bool[values.Length][];
            for (int i = 0; i < values.Length; i++)
            {
                result[i] = getCode(values[i], cutoff, order);
            }
            return (result);
        }

        private bool[][] binarize(long[] values, int cutoff, int order)
        {
            bool[][] result = new bool[values.Length][];
            for (int i = 0; i < values.Length; i++)
            {
                result[i] = getCode(values[i], cutoff, order);
            }
            return (result);
        }

        private bool[][] binarizeUnsigned(int[] values, int cutoff, int order, int count)
        {
            bool[][] result = new bool[count][];
            for (int i = 0; i < count; i++)
            {
                result[i] = getCodeUnsigned(values[i], cutoff, order);
            }
            return (result);
        }
        
        /// <summary>
        /// decodes an array of integers from the associated data stream
        /// </summary>
        /// <returns></returns>
        public int[] Decode()
        {
            BinaryReader br = new BinaryReader(this.target);
            int cnt = br.ReadInt32();
            int encodedLength = br.ReadInt32();
            int origPos = (int)target.Position;

            int[] result = new int[cnt];

            Coder coder = new Coder(this.target);
            coder.initDecoding();
            IBinaryStats[] stats = new IBinaryStats[contextCount];
            for (int i = 0; i < stats.Length; i++)
                stats[i] = new ContextBinaryStats(contextDepth);
            IBinaryStats rest = new ContextBinaryStats(contextDepth);
            IBinaryStats sgn = new ContextBinaryStats(contextDepth);
            List<bool> code = new List<bool>();
            for (int i = 0; i < cnt; i++)
            {
                code.Clear();
                int bin = 0;
                int lst = -1;
                while (!isCode(code, binarizerCutoff, binarizerOrder, ref lst))
                {
                    IBinaryStats bs = bin < stats.Length ? stats[bin] : rest;
                    if (isSgn(code, binarizerCutoff, binarizerOrder, ref lst))
                        bs = sgn;
                    bool bit = coder.decodeBit(bs.P0, bs.P1);
                    bs.UpdateBit(bit);
                    code.Add(bit);
                    bin++;
                }
                result[i] = deBinarize(code.ToArray(), binarizerCutoff, binarizerOrder);
            }

            target.Position = origPos + encodedLength;
            return (result);
        }

        /// <summary>
        /// decodes an array of integers from the associated data stream
        /// </summary>
        /// <returns></returns>
        public long[] DecodeLongs()
        {
            BinaryReader br = new BinaryReader(this.target);
            int cnt = br.ReadInt32();
            int encodedLength = br.ReadInt32();
            int origPos = (int)target.Position;

            long[] result = new long[cnt];

            Coder coder = new Coder(this.target);
            coder.initDecoding();
            IBinaryStats[] stats = new IBinaryStats[contextCount];
            for (int i = 0; i < stats.Length; i++)
                stats[i] = new ContextBinaryStats(contextDepth);
            IBinaryStats rest = new ContextBinaryStats(contextDepth);
            IBinaryStats sgn = new ContextBinaryStats(contextDepth);
            for (int i = 0; i < cnt; i++)
            {
                List<bool> code = new List<bool>();
                int bin = 0;
                int lst = -1;
                while (!isCode(code, binarizerCutoff, binarizerOrder, ref lst))
                {
                    IBinaryStats bs = bin < stats.Length ? stats[bin] : rest;
                    if (isSgn(code, binarizerCutoff, binarizerOrder, ref lst))
                        bs = sgn;
                    bool bit = coder.decodeBit(bs.P0, bs.P1);
                    bs.UpdateBit(bit);
                    code.Add(bit);
                    bin++;
                }
                result[i] = deBinarizeLong(code.ToArray(), binarizerCutoff, binarizerOrder);
            }

            target.Position = origPos + encodedLength;
            return (result);
        }

        /// <summary>
        /// decodes an array of integers from the associated data stream
        /// </summary>
        /// <returns></returns>
        public int[] DecodeUnsigned()
        {
            BinaryReader br = new BinaryReader(this.target);
            int cnt = br.ReadInt32();
            int encodedLength = br.ReadInt32();
            int origPos = (int)target.Position;

            int[] result = new int[cnt];

            Coder coder = new Coder(this.target);
            coder.initDecoding();
            IBinaryStats[] stats = new IBinaryStats[contextCount];
            for (int i = 0; i < stats.Length; i++)
                stats[i] = new ContextBinaryStats(contextDepth);
            IBinaryStats rest = new ContextBinaryStats(contextDepth);            
            for (int i = 0; i < cnt; i++)
            {
                List<bool> code = new List<bool>();
                int bin = 0;
                int lst = -1;
                if (useDirectCoding)
                {
                    while (!isCodeDirect(code, directCodeLength))
                    {
                        IBinaryStats bs = bin < stats.Length ? stats[bin] : rest;
                        bool bit = coder.decodeBit(bs.P0, bs.P1);
                        bs.UpdateBit(bit);
                        code.Add(bit);
                        bin++;
                    }
                }
                else
                {
                    while (!isCodeUnsigned(code, binarizerCutoff, binarizerOrder, ref lst))
                    {
                        IBinaryStats bs = bin < stats.Length ? stats[bin] : rest;
                        bool bit = coder.decodeBit(bs.P0, bs.P1);
                        bs.UpdateBit(bit);
                        code.Add(bit);
                        bin++;
                    }
                }
                if (useDirectCoding)
                    result[i] = deBinarizeDirect(code.ToArray());
                else
                    result[i] = deBinarizeUnsigned(code.ToArray(), binarizerCutoff, binarizerOrder);
                foreach (IBinaryStats st in stats)
                    st.UpdateValue(result[i]);
                rest.UpdateValue(result[i]);
            }

            target.Position = origPos + encodedLength;
            return (result);
        }

        private int deBinarizeDirect(bool[] p)
        {
            int m = 1;
            int result = 0;
            for (int i = 0; i < p.Length; i++)
            {
                if (p[p.Length - i - 1])
                    result += m;
                m *= 2;
            }
            return (result);
        }

        private bool isCodeDirect(List<bool> code, int directCodeLength)
        {
            return (code.Count == directCodeLength);
        }

        /// <summary>
        /// decodes an array of integers from the associated data stream
        /// </summary>
        /// <returns></returns>
        public bool[] DecodeBools()
        {
            BinaryReader br = new BinaryReader(this.target);
            int cnt = br.ReadInt32();
            int encodedLength = br.ReadInt32();
            int origPos = (int)target.Position;

            bool[] result = new bool[cnt];

            Coder coder = new Coder(this.target);
            coder.initDecoding();
            IBinaryStats stats = new ContextBinaryStats(contextDepth);
            //IBinaryStats rest = new ContextBinaryStats(contextDepth);            
            for (int i = 0; i < cnt; i++)
            {                
                bool bit = coder.decodeBit(stats.P0, stats.P1);
                stats.UpdateBit(bit);
                result[i] = bit;
            }

            target.Position = origPos + encodedLength;
            return (result);
        }
    }

    class Coder
    {
        uint L, R, D, V;
        int b = 30;
        uint quarter,half;
        Stream target;

        public Coder(Stream targetStream)
        {
            L = 0;
            D = 0;
            quarter = (uint)2 << (b - 2);
            half = (uint)2 << (b - 1);
            R = 2*half;
            target = targetStream;
        }

        public void initDecoding()
        {
            D = 0;
            V = 0;
            for (int i = 0; i <= b; i++)
            {
                
                D = D * 2;
                V = V * 2;
                bool bit = readBit();
                if (bit)
                {
                    D++;
                    V++;
                }
            }
        }

        public void finishEncode()
        {
            for (int i = 0; i < b; i++)
            {
                bool bit = ((L >> (b - i)) & 1) == 1;
                bitPlusFollow(bit);
            }
            FlushStream();
        }

        public void encodeBit(uint c0, uint c1, bool bit)
        {            
            bool LPS;
            uint cLPS = 0;
            if (c0 < c1)
            {
                LPS = false;
                cLPS = c0;
            }
            else
            {
                LPS = true;
                cLPS = c1;
            }
            uint r = R / (c0 + c1);
            uint rLPS = r * cLPS;
            if (bit == LPS)
            {
                L = L + R - rLPS;
                R = rLPS;
            }
            else
            {
                R = R - rLPS;
            }
            renormalize();
        }

        public bool decodeBit(uint c0, uint c1)
        {            
            bool LPS;
            uint cLPS = 0;
            if (c0 < c1)
            {
                LPS = false;
                cLPS = c0;
            }
            else
            {
                LPS = true;
                cLPS = c1;
            }
            uint r = R / (c0 + c1);
            uint rLPS = r * cLPS;

            bool result;

            if (D >= (R - rLPS))
            {
                result = LPS;
                D = D - (R - rLPS);
                L = L + R - rLPS;
                R = rLPS;
                
            }
            else
            {
                result = !LPS;
                R = R - rLPS;
            }
            renormalizeDecomp();            
            return result;
        }

        int bitsOutstanding = 0;

        private void renormalize()
        {
            while (R <= quarter)
            {
                if (L >= half)
                {
                    bitPlusFollow(true);
                    L -= half;
                }
                else
                {
                    if ((R+L) <= half)
                    {
                        bitPlusFollow(false);                        
                    }
                    else
                    {
                        bitsOutstanding++;
                        L = L - quarter;
                    }
                }
                L = 2 * L;
                R = 2 * R;
            }
        }

        private void renormalizeDecomp()
        {
            while (R <= quarter)
            {
                if (L >= half)
                {                    
                    L -= half;
                    V -= half;
                }
                else
                {
                    if ((R + L) <= half)
                    {                        
                    }
                    else
                    {                        
                        L = L - quarter;
                        V = V - quarter;
                    }
                }
                L = 2 * L;
                R = 2 * R;
                bool bit = readBit();
                D = 2 * D;
                V = 2 * V;
                if (bit)
                {
                    D++;
                    V++;
                }
            }
        }

        private void bitPlusFollow(bool b)
        {
            writeBit(b);
            while (bitsOutstanding > 0)
            {
                writeBit(!b);
                bitsOutstanding--;
            }
        }
        
        private byte buffer;
        private int position;

        private void FlushStream()
        {
            if (position != 0)
            {
                target.WriteByte(buffer);
                position = 0;
            }
        }

        private void writeBit(bool bit)
        {            
            if (bit)
            {
                switch (position)
                {
                    case 0: buffer += 1;
                        break;
                    case 1: buffer += 2;
                        break;
                    case 2: buffer += 4;
                        break;
                    case 3: buffer += 8;
                        break;
                    case 4: buffer += 16;
                        break;
                    case 5: buffer += 32;
                        break;
                    case 6: buffer += 64;
                        break;
                    case 7: buffer += 128;
                        break;
                }
            }
            position++;
            if (position == 8)
            {
                position = 0;
                target.WriteByte(buffer);
                buffer = 0;
            }
        }

        private bool readBit()
        {
            if (position == 0)
            {
                buffer = (Byte)target.ReadByte();
            }
            
            bool result = false;
            switch (position)
            {
                case 0:
                    result = ((buffer & 1)>0);
                    break;
                case 1:
                    result = ((buffer & 2) > 0);
                    break;
                case 2:
                    result = ((buffer & 4) > 0);
                    break;
                case 3:
                    result = ((buffer & 8) > 0);
                    break;
                case 4:
                    result = ((buffer & 16) > 0);
                    break;
                case 5:
                    result = ((buffer & 32) > 0);
                    break;
                case 6:
                    result = ((buffer & 64) > 0);
                    break;
                case 7:
                    result = ((buffer & 128) > 0);
                    break;
            }            
            position++;
            if (position == 8)
                position = 0;
            return (result);
        }        
    }

    class QuantizedStats : IBinaryStats
    {
        int internalState = 63;

        static int[] p0, p1, trans0, trans1;

        static QuantizedStats()
        {
            p0 = new int[127];
            p1 = new int[127];
            trans0 = new int[127];
            trans1 = new int[127];
            double p = 0.5;
            double a = Math.Pow(0.01875/0.5,1/63.0);
            p0[63] = 512;
            p1[63] = 512;
            p1[63] = 512;
            p0[63] = 512;
            trans0[63] = 62;
            trans1[63] = 64;
            for (int i = 1; i < 64; i++)
            {
                p = p * a;
                p0[63 + i] = (int)(1024 * p);
                p1[63 + i] = 1024 - p0[63 + i];
                p1[63 - i] = p0[63 + i];
                p0[63 - i] = p1[63 + i];
                trans1[63 + i] = 63 + i + 1;
                trans0[63 - i] = 63 - i - 1;
                double pnew = a * p + (1 - a);
                int desired = (int)(1024*pnew);
                int index = 63 + i;
                while (p0[index - 1] < desired)
                    index--;
                trans0[63 + i] = index;
                trans1[63 - i] = 126 - index;
            }
            trans1[126] = 126;
            trans0[0] = 0;
        }

        #region IBinaryStats Members

        public uint P0
        {
            get
            {
                return ((uint)p0[internalState]);
            }            
        }

        public uint P1
        {
            get
            {
                return ((uint)p1[internalState]);
            }            
        }

        public void UpdateBit(bool val)
        {
            if (val)
            {
                internalState = trans1[internalState];
            }
            else
            {
                internalState = trans0[internalState];
            }
        }

        public void UpdateValue(int val)
        {
        }

        public QuantizedStats()
        {
        }

        public QuantizedStats(int index, int count)
        {
            int zeros = 1;
            int ones = 1;
            for (int i = 0; i < count; i++)
            {
                if (((index >> i) & 1)>0)
                {
                    ones++;
                }
                else
                {
                    zeros++;
                }
            }
            int cnt = 1 << count;
            zeros = zeros * 1024 / (count+2);
            ones = ones * 1024 / (count+2);
            internalState = 0;
            while ((internalState<12)&&(zeros < p0[internalState]))
                internalState++;
        }


        #endregion
    }

    class ValueContextBinaryStats : IBinaryStats
    {
        QuantizedStats[] stats;

        int context = 0;
        int mask;

        public ValueContextBinaryStats(int order)
        {
            int count = 1 << order;
            mask = count - 1;
            stats = new QuantizedStats[count];
            for (int i = 0; i < count; i++)
            {
                stats[i] = new QuantizedStats();                
            }
        }

        #region IBinaryStats Members

        public uint P0
        {
            get {
                return (stats[context].P0);
            }
        }

        public uint P1
        {
            get { return (stats[context].P1);}
        }

        public void UpdateBit(bool val)
        {
            stats[context].UpdateBit(val);            
        }

        public void UpdateValue(int val)
        {
            context = val;
        }

        #endregion
    }

    class ContextBinaryStats : IBinaryStats
    {
        QuantizedStats[] stats;

        int context = 0;
        int mask;

        public ContextBinaryStats(int order)
        {
            int count = 1 << order;
            mask = count - 1;
            stats = new QuantizedStats[count];
            for (int i = 0; i < count; i++)
            {
                stats[i] = new QuantizedStats();                
            }
        }

        #region IBinaryStats Members

        public uint P0
        {
            get {
                return (stats[context].P0);
            }
        }

        public uint P1
        {
            get { return (stats[context].P1);}
        }

        public void UpdateBit(bool val)
        {
            stats[context].UpdateBit(val);
            context *= 2;
            if (val) context++;
            context = context & mask;
        }

        public void UpdateValue(int val)
        {
        }

        #endregion
    }

    class BinaryStats : IBinaryStats
    {
        private uint p0 = 1;

        public uint P0
        {
            get { return p0; }
            set { p0 = value; }
        }
        private uint p1 = 1;

        public uint P1
        {
            get { return p1; }
            set { p1 = value; }
        }

        public BinaryStats()
        {            
        }

        public void UpdateBit(bool val)
        {
            if (val)
                p1++;
            else
                p0++;
        }

        public void UpdateValue(int val)
        {
        }

        public bool getBit(int target)
        {
            if (target < p0)
                return (false);
            else
                return (true);
        }
    }
}
