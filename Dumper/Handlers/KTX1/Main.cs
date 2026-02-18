using System.Runtime.InteropServices;
using KTX1.ETC;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace KTX1;

public static class KtxRipper
{
    public enum KtxTextureFormat
    {
        UNSUPPORTED = 0x0,
        
        RGB8_ETC1 = 0x8D64,
        
        R11_EAC = 0x9270,
        SIGNED_R11_EAC = 0x9271,
        
        RG11_EAC = 0x9272,
        SIGNED_RG11_EAC = 0x9273,
        
        RGB8_ETC2 = 0x9274,
        SRGB8_ETC2 = 0x9275,
        
        RGB8_PUNCHTHROUGH_ALPHA1_ETC2 = 0x9276,
        SRGB8_PUNCHTHROUGH_ALPHA1_ETC2 = 0x9277,
        
        RGBA8_ETC2_EAC = 0x9278,
        SRGB8_ALPHA8_ETC2_EAC = 0x9279
    }
    
    private static uint CeilMul(uint value, uint multiplier)
    {
        return ((value + (multiplier - 1)) / multiplier) * multiplier;
    }
    
    public static KtxTextureFormat FormatFrom(uint format)
    {
        try
        {
            return (KtxTextureFormat)format;
        }
        catch
        {
            return KtxTextureFormat.UNSUPPORTED;
        }
    }
    
    public class KtxTexture
    {
        private byte[] Data;
        private uint InternalFormat;
        
        public KtxTextureFormat Format;
        public uint Width;
        public uint Height;
        
        public uint RealWidth;
        public uint RealHeight;
        
        public static KtxTexture OpenFromMemory(byte[] data)
        {
            var ms = new ByteReader(data);
            ms.SkipBytes(12 + 4 + 4 + 4 + 4); // assuming the header is correct
            
            var internalFormat = ms.ReadU32();
            var format = FormatFrom(internalFormat);
            
            ms.SkipBytes(4); // useless data
            
            var width = ms.ReadU32();
            var height = ms.ReadU32();
            
            // blocks are 4x4, so we need to account for that if the image size isn't a multiple of 4
            var compressedWidth = CeilMul(width, 4);
            var compressedHeight = CeilMul(height, 4);
            
            ms.SkipBytes(4 + 4 + 4 + 4); // useless data
            
            var kvDataSize = ms.ReadU32();
            ms.SkipBytes(kvDataSize); // we don't need any of this
            
            var dataSize = ms.ReadU32();
            var imageData = ms.ReadBytes(dataSize);
            
            return new KtxTexture
            {
                InternalFormat = internalFormat,
                Format = format,
                
                Width = compressedWidth,
                Height = compressedHeight,
                RealWidth = width,
                RealHeight = height,
                
                Data = imageData
            };
        }
        
        public byte[] Decode()
        {
            var intWidth = (int)Width;
            var intHeight = (int)Height;
            
            if (Format == KtxTextureFormat.RGB8_ETC1 || Format == KtxTextureFormat.RGB8_ETC2 || Format == KtxTextureFormat.SRGB8_ETC2)
            {
                var size = intWidth * intHeight * 4;
                var destination = new byte[size];
                
                var sourceOffset = 0;
                var destinationOffset = 0;
                var destinationPitch = intWidth * 4;
                
                for (int blockY = 0; blockY < intHeight; blockY += 4)
                {
                    for (int blockX = 0; blockX < intWidth; blockX += 4)
                    {
                        destinationOffset = (blockY * intWidth + blockX) * 4;
                        var source = Data[sourceOffset..];
                        ETCDecompress.EtcRGB(source, destination.AsSpan(destinationOffset), destinationPitch);
                        sourceOffset += 8;
                    }
                }
                
                return destination;
            }
            else if (Format == KtxTextureFormat.RGBA8_ETC2_EAC || Format == KtxTextureFormat.SRGB8_ALPHA8_ETC2_EAC)
            {
                var size = intWidth * intHeight * 4;
                var destination = new byte[size];
                
                var sourceOffset = 0;
                var destinationOffset = 0;
                var destinationPitch = intWidth * 4;
                
                for (int blockY = 0; blockY < intHeight; blockY += 4)
                {
                    for (int blockX = 0; blockX < intWidth; blockX += 4)
                    {
                        destinationOffset = (blockY * intWidth + blockX) * 4;
                        var source = Data[sourceOffset..];
                        ETCDecompress.EacRGBA(source, destination.AsSpan(destinationOffset), destinationPitch);
                        sourceOffset += 16;
                    }
                }
                
                return destination;
            }
            
            throw new NotSupportedException($"This KTX1.1 format ({InternalFormat}) is not supported!");
        }
        
        public void SaveToPng(string path)
        {
            byte[] decodedData = Decode();
            
            var stride = (int)Width * 4;
            using var output = new Image<Rgba32>((int)RealWidth, (int)RealHeight);
            
            for (int y = 0; y < RealHeight; y++)
            {
                Span<Rgba32> row = output.Frames.RootFrame.PixelBuffer.DangerousGetRowSpan(y);
                ReadOnlySpan<byte> source = new(decodedData, y * stride, (int)RealWidth * 4);
                MemoryMarshal.Cast<byte, Rgba32>(source).CopyTo(row);
            }
            
            output.SaveAsPng(path);
        }
    }
}