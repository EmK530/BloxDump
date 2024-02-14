// Script made by zuzaratrust and EmK530

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public static class srgb2lin
{
    static byte[] preComputed = new byte[256];
    static double factor = 1.0 / 2058.61501702;

    static double tolin(int s)
    {
        double lin;
        if (s < 11)
        {
            lin = (double)s / 12.92;
        }
        else
        {
            lin = Math.Pow(((double)s + 0.055) / 1.055, 2.4);
        }
        return lin;
    }

    static bool computed = false;

    static void preCompute()
    {
        for(int i = 0; i < 256; i++)
        {
            preComputed[i] = (byte)Math.Floor(tolin(i) * factor);
        }
        computed = true;
    }

    public static void convert(string outPath)
    {
        if (!computed)
        {
            preCompute();
        }

        Image<Rgba32> img = Image.Load<Rgba32>(outPath);

        for (int y = 0; y < img.Height; y++)
        {
            for (int x = 0; x < img.Width; x++)
            {
                Rgba32 px = img[x, y];
                px.R = preComputed[px.R];
                px.G = preComputed[px.G];
                px.B = preComputed[px.B];
                img[x, y] = px;
            }
        }

        img.Save(outPath);
    }
}
