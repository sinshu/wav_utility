using System;
using System.IO;

public static class WavUtil
{
    private const int RiffHeader = 0x46464952;
    private const int WaveHeader = 0x45564157;
    private const int FmtHeader = 0x20746D66;
    private const int DataHeader = 0x61746164;

    public static double[][] Read(string fileName, out int sampFreq)
    {
        using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
        {
            using (BinaryReader br = new BinaryReader(fs))
            {
                if (br.ReadInt32() != RiffHeader)
                {
                    throw new Exception("WAVファイルじゃない＼(^o^)／");
                }
                br.ReadInt32();
                if (br.ReadInt32() != WaveHeader)
                {
                    throw new Exception("WAVファイルじゃない＼(^o^)／");
                }

                while (br.ReadInt32() != FmtHeader)
                {
                    br.BaseStream.Seek(br.ReadInt32(), SeekOrigin.Current);
                }

                int numChannels;
                {
                    byte[] chunk = new byte[br.ReadInt32()];
                    br.Read(chunk, 0, chunk.Length);
                    ReadFmtChunk(chunk, out numChannels, out sampFreq);
                }

                while (br.ReadInt32() != DataHeader)
                {
                    br.BaseStream.Seek(br.ReadInt32(), SeekOrigin.Current);
                }

                {
                    byte[] chunk = new byte[br.ReadInt32()];
                    br.Read(chunk, 0, chunk.Length);
                    return ReadDataChunk(chunk, numChannels);
                }
            }
        }
    }

    public static double[][] Read(string fileName)
    {
        int sampFreq;
        return Read(fileName, out sampFreq);
    }

    public static double[] ReadMono(string fileName, out int sampFreq)
    {
        return Read(fileName, out sampFreq)[0];
    }

    public static double[] ReadMono(string fileName)
    {
        int sampFreq;
        return ReadMono(fileName, out sampFreq);
    }

    public static void Write(string fileName, double[][] data, int sampFreq)
    {
        int numChannels = data.Length;
        int pcmLength = data[0].Length;
        int dataChunkSize = 2 * numChannels * pcmLength;
        int riffChunkSize = dataChunkSize + 36;
        bool clipped = false;
        using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
        {
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write(RiffHeader);
                bw.Write(riffChunkSize);
                bw.Write(WaveHeader);
                bw.Write(FmtHeader);
                bw.Write(16);
                bw.Write((short)1);
                bw.Write((short)numChannels);
                bw.Write(sampFreq);
                bw.Write(2 * numChannels * sampFreq);
                bw.Write((short)(2 * numChannels));
                bw.Write((short)16);
                bw.Write(DataHeader);
                bw.Write(dataChunkSize);
                for (int t = 0; t < pcmLength; t++)
                {
                    for (int ch = 0; ch < numChannels; ch++)
                    {
                        int s = (int)Math.Floor(32768 * data[ch][t]);
                        if (s < short.MinValue)
                        {
                            s = short.MinValue;
                            clipped = true;
                        }
                        else if (s > short.MaxValue)
                        {
                            s = short.MaxValue;
                            clipped = true;
                        }
                        bw.Write((short)s);
                    }
                }
            }
        }
        if (clipped)
        {
            Console.Error.WriteLine("クリップした／(^o^)＼");
        }
    }

    public static void Write(string fileName, double[] data, int sampFreq)
    {
        Write(fileName, new double[][] { data }, sampFreq);
    }

    public static void Normalize(double[][] data)
    {
        double max = double.MinValue;
        foreach (double[] d in data)
        {
            foreach (double s in d)
            {
                if (max < s)
                {
                    max = s;
                }
            }
        }
        for (int ch = 0; ch < data.Length; ch++)
        {
            for (int t = 0; t < data[ch].Length; t++)
            {
                data[ch][t] = 0.99 * data[ch][t] / max;
            }
        }
    }

    public static void Normalize(double[] data)
    {
        Normalize(new double[][] { data });
    }

    private static void ReadFmtChunk(byte[] chunk, out int numChannels, out int sampFreq)
    {
        using (MemoryStream ms = new MemoryStream(chunk))
        {
            using (BinaryReader br = new BinaryReader(ms))
            {
                int formatId = br.ReadInt16();
                if (formatId != 1)
                {
                    throw new Exception("対応してないフォーマット＼(^o^)／");
                }
                numChannels = br.ReadInt16();
                sampFreq = br.ReadInt32();
                br.ReadInt32();
                br.ReadInt16();
                int quantBit = br.ReadInt16();
                if (quantBit != 16)
                {
                    throw new Exception("対応してない量子化ビット数＼(^o^)／");
                }
            }
        }
        Console.WriteLine("numChannels: " + numChannels);
        Console.WriteLine("sampFreq: " + sampFreq);
    }

    private static double[][] ReadDataChunk(byte[] chunk, int numChannels)
    {
        int pcmLength = chunk.Length / (2 * numChannels);
        double[][] dst = new double[numChannels][];
        for (int ch = 0; ch < numChannels; ch++)
        {
            dst[ch] = new double[pcmLength];
        }
        using (MemoryStream ms = new MemoryStream(chunk))
        {
            using (BinaryReader br = new BinaryReader(ms))
            {
                for (int t = 0; t < pcmLength; t++)
                {
                    for (int ch = 0; ch < numChannels; ch++)
                    {
                        dst[ch][t] = br.ReadInt16() / 32768.0;
                    }
                }
            }
        }
        return dst;
    }
}
