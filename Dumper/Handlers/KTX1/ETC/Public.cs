namespace KTX1.ETC;

public static partial class ETCDecompress
{
    public static void EtcRGB(byte[] source, Span<byte> destination, int pitch)
    {
        DecompressETCBlock(source, destination, false, pitch);
    }
    
    public static void EacRGBA(byte[] source, Span<byte> destination, int pitch)
    {
        DecompressETCBlock(source.AsSpan(8), destination, false, pitch); // Colors
        DecompressEACBlock(source, destination[3..], false, pitch, 4); // Alpha
    }
}