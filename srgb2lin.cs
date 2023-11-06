// Script made by zuzaratrust and EmK530

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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
        Image<Rgba32> img = Image.Load<Rgba32>(outPath);
        int y = -1;
        int x = 0;
        for (int i = 0; i < img.Height * img.Width; i++)
        {
            y++;
            if (y == img.Height) {y = 0;x++;}
            Rgba32 px = img[x, y];
            img[x, y] = new Rgba32(
                (byte)Math.Floor(srgb2lin(px.R) / 2058.61501702),
                (byte)Math.Floor(srgb2lin(px.G) / 2058.61501702),
                (byte)Math.Floor(srgb2lin(px.B) / 2058.61501702),
                px.A
            );
        }
        img.Save(outPath);
    }
}
