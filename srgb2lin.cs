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
        byte[,,] data = (byte[,,])img.Data.Clone();
        int height = data.GetLength(0);
        int width = data.GetLength(1);
        for (int i = 0; i < height * width; i++)
        {
            int y = i % height;
            int x = (int)Math.Floor((double)(i / height));
            for (int c = 0; c < 3; c++)
            {
                data[y, x, c] = (byte)Math.Floor(srgb2lin(data[y, x, c]) / 2058.61501702);
            }
        }
        Image<Bgra, byte> newi = new Image<Bgra, byte>(data);
        newi.Save(outPath);
    }
}
