// Script made by zuzaratrust and EmK530

using Emgu.CV;
using Emgu.CV.Structure;

public static class srgb2lin
{
    public static void convert(string outPath)
    {
        double srgb2lin(int s)
        {
            double lin;
            if (s <= 0.0404482362771082)
            {
                lin = (double)s / 12.92;
            }
            else
            {
                lin = Math.Pow(((s + 0.055) / 1.055), 2.4);
            }
            return lin;
        }
        Image<Bgra, byte> img = new Image<Bgra, byte>(outPath);
        int height = img.Data.GetLength(0);
        int width = img.Data.GetLength(1);
        int y = -1;
        int x = 0;
        for (int i = 0; i < height * width; i++)
        {
            y++;
            if (y == height){y = 0;x++;}
            for (int c = 0; c < 3; c++)
            {
                img.Data[y, x, c] = (byte)Math.Floor(srgb2lin(img.Data[y, x, c]) / 2058.61501702);
            }
        }
        img.Save(outPath);
    }
}
