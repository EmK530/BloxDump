using System;
using System.Runtime.InteropServices;

namespace Ktx2Sharp;

public static unsafe partial class Ktx
{
    [StructLayout(LayoutKind.Sequential)]
    public struct KtxTexture
    {
        //[FieldOffset(0)]
        public ClassId ClassId;
        //[FieldOffset(4)]
        public IntPtr VTable;
        //[FieldOffset(12)]
        public IntPtr VTableVulkan;
        //[FieldOffset(20)]
        public IntPtr Protected;


        //[FieldOffset(28)]
        public bool IsArray;
        //[FieldOffset(29)]
        public bool IsCubeMap;
        //[FieldOffset(30)]
        public bool IsCompressed;
        //[FieldOffset(31)]
        public bool GenerateMipmaps;
        //[FieldOffset(32)]
        public uint BaseWidth;
        //[FieldOffset(36)]
        public uint BaseHeight;
        //[FieldOffset(40)]
        public uint BaseDepth;
        //[FieldOffset(44)]
        public uint NumDimensions;
        //[FieldOffset(48)]
        public uint NumLevels;
        //[FieldOffset(52)]
        public uint NumLayers;
        //[FieldOffset(56)]
        public uint NumFaces;
        //[FieldOffset(60)]
        public Orientation Orientation;

        public IntPtr KeyValueDataHead;
        //[FieldOffset(64)]
        public uint KeyValueDataLength;
        //[FieldOffset(68)]
        public void* KeyValueData;
        //[FieldOffset(76)]
        public uint DataSize;
        //[FieldOffset(80)]
        public void* Data;

        //[FieldOffset(88)]
        public VkFormat VulkanFormat;


        //[FieldOffset(92)]
        public void* Dfd;

        //[FieldOffset(96)]
        public SuperCompressionScheme CompressionScheme;
        //[FieldOffset(100)]
        public bool IsVideo;
        //[FieldOffset(101)]
        public uint VideoDuration;
        //[FieldOffset(105)]
        public uint VideoTimeScale;
        //[FieldOffset(109)]
        public uint VideoLoopCount;
        //[FieldOffset(113)]
        public void* PrivateData;
    }

    public struct Orientation
    {
        public KtxOrientationX OrientationX;
        public KtxOrientationY OrientationY;
        public KtxOrientationZ OrientationZ;
    }

    public struct GLFormatInfo
    {
        public uint Format;
        public uint Internalformat;
        public uint BaseInternalformat;
        public uint Type;
    }
}