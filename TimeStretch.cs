using System;

public class TimeStretch
{
    public static double[] Stretch(double[] src, double ratio, int frameLength, int overlapLength, int searchLength)
    {
        double[] dst = new double[(int)(ratio * src.Length)];

        Array.Copy(src, dst, frameLength);
        int curDstEndIndex = frameLength;

        while (true)
        {
            int srcStartIndex = (int)((double)curDstEndIndex / dst.Length * src.Length);
            int connectStartIndex = FindConnectStartIndex(dst, curDstEndIndex, src, srcStartIndex, overlapLength, searchLength);
            for (int t = 0; t < overlapLength; t++)
            {
                if (connectStartIndex + t == dst.Length || srcStartIndex + t == src.Length) return dst;
                double a = (double)t / overlapLength;
                dst[connectStartIndex + t] = a * src[srcStartIndex + t] + (1 - a) * dst[connectStartIndex + t];
            }
            for (int t = overlapLength; t < frameLength; t++)
            {
                if (connectStartIndex + t == dst.Length || srcStartIndex + t == src.Length) return dst;
                dst[connectStartIndex + t] = src[srcStartIndex + t];
            }
            curDstEndIndex = connectStartIndex + frameLength;
        }
    }

    private static double CalcDifference(double[] dst, int dstStartIndex, double[] src, int srcStartIndex, int overlapLength)
    {
        double sum = 0;
        for (int t = 0; t < overlapLength; t++)
        {
            double d = dst[dstStartIndex + t] - src[srcStartIndex + t];
            sum += d * d;
        }
        return sum;
    }

    private static int FindConnectStartIndex(double[] dst, int dstEndIndex, double[] src, int srcStartIndex, int overlapLength, int searchLength)
    {
        int dstSearchStartIndex = dstEndIndex - overlapLength - searchLength;
        double minDiff = double.MaxValue;
        int minDiffIndex = new int();
        for (int t = 0; t < searchLength; t++)
        {
            double diff = CalcDifference(dst, dstSearchStartIndex + t, src, srcStartIndex, overlapLength);
            if (diff < minDiff)
            {
                minDiff = diff;
                minDiffIndex = dstSearchStartIndex + t;
            }
        }
        return minDiffIndex;
    }
}
