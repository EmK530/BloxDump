using System;
using System.Runtime.InteropServices;

namespace Ktx2Sharp;

public static unsafe partial class Ktx
{
    private static delegate* unmanaged<IntPtr, KtxTextureCreateFlagBits, KtxTexture**, KtxErrorCode> _ktxTexture2CreateFromNamedFileDelegate = &ktxTexture2_CreateFromNamedFile;
    private static delegate* unmanaged<void*, IntPtr, KtxTextureCreateFlagBits, KtxTexture**, KtxErrorCode> _ktxTexture2CreateFromMemoryDelegate = &ktxTexture2_CreateFromMemory;

    private static delegate* unmanaged<KtxTexture*, void> _ktxTexture2DestroyDelegate = &ktxTexture2_Destroy;
    private static delegate* unmanaged<KtxTexture*, int> _ktxTexture2NeedsTranscodingDelegate = &ktxTexture2_NeedsTranscoding;
    private static delegate* unmanaged<KtxTexture*, uint> _ktxTexture2GetNumComponentsDelegate = &ktxTexture2_GetNumComponents;

    private static delegate* unmanaged<KtxTexture*, uint, uint, uint, uint*, KtxErrorCode> _ktxTexture2GetImageOffsetDelegate = &ktxTexture2_GetImageOffset;

    private static delegate* unmanaged<KtxTexture*, uint, uint> _ktxTexture2GetImageSizeDelegate = &ktxTexture2_GetImageSize;

    private static delegate* unmanaged<KtxTexture*, TranscodeFormat, TranscodeFlagBits, KtxErrorCode> _ktxTexture2TranscodeBasisDelegate = &ktxTexture2_TranscodeBasis;

    [UnmanagedCallersOnly]
    private static void ktxTexture2_Destroy(KtxTexture* texture)
    {
        _ktxTexture2DestroyDelegate = (delegate* unmanaged<KtxTexture*, void>)NativeLibrary.GetExport(_nativeLibraryHandle, nameof(ktxTexture2_Destroy));
        _ktxTexture2DestroyDelegate(texture);
    }

    [UnmanagedCallersOnly]
    private static KtxErrorCode ktxTexture2_CreateFromMemory(
        void* data,
        IntPtr size,
        KtxTextureCreateFlagBits textureCreateFlags,
        KtxTexture** texturePtr)
    {
        _ktxTexture2CreateFromMemoryDelegate =
            (delegate* unmanaged<void*, IntPtr, KtxTextureCreateFlagBits, KtxTexture**, KtxErrorCode>)NativeLibrary.GetExport(_nativeLibraryHandle,
                nameof(ktxTexture2_CreateFromMemory));
        return _ktxTexture2CreateFromMemoryDelegate(data, size, textureCreateFlags, texturePtr);
    }

    [UnmanagedCallersOnly]
    private static KtxErrorCode ktxTexture2_CreateFromNamedFile(
        IntPtr fileNamePtr,
        KtxTextureCreateFlagBits textureCreateFlags,
        KtxTexture** texturePtr)
    {
        _ktxTexture2CreateFromNamedFileDelegate =
            (delegate* unmanaged<IntPtr, KtxTextureCreateFlagBits, KtxTexture**, KtxErrorCode>)NativeLibrary.GetExport(_nativeLibraryHandle,
                nameof(ktxTexture2_CreateFromNamedFile));
        return _ktxTexture2CreateFromNamedFileDelegate(fileNamePtr, textureCreateFlags, texturePtr);
    }

    [UnmanagedCallersOnly]
    private static int ktxTexture2_NeedsTranscoding(KtxTexture* texture)
    {
        _ktxTexture2NeedsTranscodingDelegate = (delegate* unmanaged<KtxTexture*, int>)NativeLibrary.GetExport(_nativeLibraryHandle,
            nameof(ktxTexture2_NeedsTranscoding));
        return _ktxTexture2NeedsTranscodingDelegate(texture);
    }

    [UnmanagedCallersOnly]
    private static KtxErrorCode ktxTexture2_TranscodeBasis(
        KtxTexture* texture,
        TranscodeFormat transcodeFormat,
        TranscodeFlagBits transcodeFlagBits)
    {
        _ktxTexture2TranscodeBasisDelegate = (delegate* unmanaged<KtxTexture*, TranscodeFormat, TranscodeFlagBits, KtxErrorCode>)NativeLibrary.GetExport(_nativeLibraryHandle,
            nameof(ktxTexture2_TranscodeBasis));
        return _ktxTexture2TranscodeBasisDelegate(texture, transcodeFormat, transcodeFlagBits);
    }

    [UnmanagedCallersOnly]
    private static uint ktxTexture2_GetNumComponents(KtxTexture* texture)
    {
        _ktxTexture2GetNumComponentsDelegate = (delegate* unmanaged<KtxTexture*, uint>)NativeLibrary.GetExport(_nativeLibraryHandle,
            nameof(ktxTexture2_GetNumComponents));
        return _ktxTexture2GetNumComponentsDelegate(texture);
    }

    [UnmanagedCallersOnly]
    private static KtxErrorCode ktxTexture2_GetImageOffset(
        KtxTexture* texture,
        uint mipLevel,
        uint layer,
        uint faceIndex,
        uint* imageOffset)
    {
        _ktxTexture2GetImageOffsetDelegate =
            (delegate* unmanaged<KtxTexture*, uint, uint, uint, uint*, KtxErrorCode>)NativeLibrary.GetExport(
                _nativeLibraryHandle, nameof(ktxTexture2_GetImageOffset));
        return _ktxTexture2GetImageOffsetDelegate(texture, mipLevel, layer, faceIndex, imageOffset);
    }

    [UnmanagedCallersOnly]
    private static uint ktxTexture2_GetImageSize(
        KtxTexture* texture,
        uint mipLevel)
    {
        _ktxTexture2GetImageSizeDelegate =
            (delegate* unmanaged<KtxTexture*, uint, uint>)NativeLibrary.GetExport(
                _nativeLibraryHandle, nameof(ktxTexture2_GetImageSize));
        return _ktxTexture2GetImageSizeDelegate(texture, mipLevel);
    }
}