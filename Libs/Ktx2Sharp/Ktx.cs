using BCnEncoder.Shared;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ktx2Sharp;

public static partial class Ktx
{
    private const string LibraryName = "ktx";

    private static IntPtr _nativeLibraryHandle = IntPtr.Zero;

    private const string SrLibKtxNotInitialized =
        "Ktx not initialized. Call Ktx.Init somewhere in your application startup first";

    public static bool Init()
    {
        var executingAssembly = typeof(Ktx).Assembly;
        if (NativeLibrary.TryLoad(LibraryName, executingAssembly, DllImportSearchPath.AssemblyDirectory, out _nativeLibraryHandle))
        {
            return true;
        }

        Debug.WriteLine("Ktx: Unable to load native library");
        return false;
    }

    public static void Terminate()
    {
        if (_nativeLibraryHandle != IntPtr.Zero)
        {
            NativeLibrary.Free(_nativeLibraryHandle);
        }
    }

    public static unsafe KtxTexture* LoadFromMemory(ReadOnlyMemory<byte> data)
    {
        if (_nativeLibraryHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException(SrLibKtxNotInitialized);
        }
        KtxTexture* ktxTexture = null;
        var createFlagBits = KtxTextureCreateFlagBits.LoadImageDataBit;
        var result = _ktxTexture2CreateFromMemoryDelegate(data.Pin().Pointer, (IntPtr)data.Length, createFlagBits, &ktxTexture);
        return result != KtxErrorCode.KtxSuccess ? null : ktxTexture;
    }

    public static unsafe KtxTexture* LoadFromFile(string fileName)
    {
        if (_nativeLibraryHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException(SrLibKtxNotInitialized);
        }
        var fileNamePtr = Marshal.StringToHGlobalAnsi(fileName);
        KtxTexture* ktxTexture = null;
        var createFlagBits = KtxTextureCreateFlagBits.LoadImageDataBit;
        var result = _ktxTexture2CreateFromNamedFileDelegate(fileNamePtr, createFlagBits, &ktxTexture);
        Marshal.FreeHGlobal(fileNamePtr);
        return result != KtxErrorCode.KtxSuccess ? null : ktxTexture;
    }

    public static unsafe void Destroy(KtxTexture* texture)
    {
        if (_nativeLibraryHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException(SrLibKtxNotInitialized);
        }
        _ktxTexture2DestroyDelegate(texture);
    }

    public static unsafe bool NeedsTranscoding(KtxTexture* texture)
    {
        if (_nativeLibraryHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException(SrLibKtxNotInitialized);
        }
        return _ktxTexture2NeedsTranscodingDelegate(texture) == 1;
    }

    public static unsafe KtxErrorCode Transcode(KtxTexture* texture, TranscodeFormat transcodeFormat, TranscodeFlagBits transcodeFlagBits)
    {
        if (_nativeLibraryHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException(SrLibKtxNotInitialized);
        }
        return _ktxTexture2TranscodeBasisDelegate(texture, transcodeFormat, transcodeFlagBits);
    }

    public static unsafe uint GetNumComponents(KtxTexture* texture)
    {
        if (_nativeLibraryHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException(SrLibKtxNotInitialized);
        }
        return _ktxTexture2GetNumComponentsDelegate(texture);
    }

    public static unsafe uint GetImageOffset(KtxTexture* texture, uint mipLevel, uint layer, uint faceIndex)
    {
        if (_nativeLibraryHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException(SrLibKtxNotInitialized);
        }
        uint imageOffset = 0;
        var result = _ktxTexture2GetImageOffsetDelegate(texture, mipLevel, layer, faceIndex, &imageOffset);
        if (result == KtxErrorCode.KtxSuccess)
        {
            return imageOffset;
        }

        throw new InvalidOperationException("Handle this properly");
    }

    public static unsafe uint GetImageSize(KtxTexture* texture, uint mipLevel)
    {
        if (_nativeLibraryHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException(SrLibKtxNotInitialized);
        }
        return _ktxTexture2GetImageSizeDelegate(texture, mipLevel);
    }

    public static CompressionFormat ConvertFormat(VkFormat vk)
    {
        return vk switch
        {
            VkFormat.Bc1RgbUnormBlock => CompressionFormat.Bc1,
            VkFormat.Bc1RgbSrgbBlock => CompressionFormat.Bc1,
            VkFormat.Bc1RgbaUnormBlock => CompressionFormat.Bc1,
            VkFormat.Bc1RgbaSrgbBlock => CompressionFormat.Bc1,
            VkFormat.Bc2UnormBlock => CompressionFormat.Bc2,
            VkFormat.Bc2SrgbBlock => CompressionFormat.Bc2,
            VkFormat.Bc3UnormBlock => CompressionFormat.Bc3,
            VkFormat.Bc3SrgbBlock => CompressionFormat.Bc3,
            VkFormat.Bc4UnormBlock => CompressionFormat.Bc4,
            VkFormat.Bc4SnormBlock => CompressionFormat.Bc4,
            VkFormat.Bc5UnormBlock => CompressionFormat.Bc5,
            VkFormat.Bc5SnormBlock => CompressionFormat.Bc5,
            VkFormat.Bc6HUfloatBlock => CompressionFormat.Bc6U,
            VkFormat.Bc6HSfloatBlock => CompressionFormat.Bc6S,
            VkFormat.Bc7UnormBlock => CompressionFormat.Bc7,
            VkFormat.Bc7SrgbBlock => CompressionFormat.Bc7,
            _ => throw new InvalidOperationException($"Unsupported BC format {vk}")
        };
    }

    public static int GetBlockSize(VkFormat vk)
    {
        return vk switch
        {
            VkFormat.Bc1RgbUnormBlock => 8,
            VkFormat.Bc1RgbSrgbBlock => 8,
            VkFormat.Bc1RgbaUnormBlock => 8,
            VkFormat.Bc1RgbaSrgbBlock => 8,
            VkFormat.Bc2UnormBlock => 16,
            VkFormat.Bc2SrgbBlock => 16,
            VkFormat.Bc3UnormBlock => 16,
            VkFormat.Bc3SrgbBlock => 16,
            VkFormat.Bc4UnormBlock => 8,
            VkFormat.Bc4SnormBlock => 8,
            VkFormat.Bc5UnormBlock => 16,
            VkFormat.Bc5SnormBlock => 16,
            VkFormat.Bc6HUfloatBlock => 16,
            VkFormat.Bc6HSfloatBlock => 16,
            VkFormat.Bc7UnormBlock => 16,
            VkFormat.Bc7SrgbBlock => 16,
            _ => throw new InvalidOperationException($"Unsupported BC format {vk}")
        };
    }
}