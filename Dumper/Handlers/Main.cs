#pragma warning disable CS8600,CS8602,CS8604

using BCnEncoder.Decoder;
using BCnEncoder.Shared;
using BCnEncoder.Shared.ImageFiles;
using CommunityToolkit.HighPerformance;
using Ktx2Sharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;

using static Essentials;
using static Ktx2Sharp.Ktx;

public static class EXTM3U
{
    public static async Task Process(int whoami, string dumpName, byte[] cont, string link = "")
    {
        if(File.Exists($"assets/Videos/{dumpName}.webm"))
        {
            debug($"Thread-{whoami}: Skipping already dumped VideoFrame.");
            return;
        }
        print($"Thread-{whoami}: Dumping asset type: VideoFrame");
        string content = Encoding.UTF8.GetString(cont);
        int tries;
        if (content.Contains("EXT-X-INDEPENDENT-SEGMENTS"))
        {
            debug($"Thread-{whoami}: Ignoring unsupported EXTM3U file.");
            return;
        }
        string hash = dumpName; //string hash = M3Data.Item3.Split("/").Last();
        //string file = link.Split("/").Last();
        //string segmentUrl = link.Replace(file, "");
        List<string> segments = new List<string>();
        bool nextIsASegment = false;
        foreach (string i in content.Split("\n"))
        {
            if (nextIsASegment)
            {
                segments.Add(i);
                nextIsASegment = false;
            }
            else
            {
                if (i.StartsWith("#EXTINF:"))
                {
                    nextIsASegment = true;
                }
            }
        }
        string tempdir = "temp/VideoFrame-" + hash;
        if (!Directory.Exists(tempdir))
        {
            Directory.CreateDirectory(tempdir);
        }

        var result = Essentials.UrlUtils.ParseUrl(link);

        List<string> names = new List<string>();
        bool succeeded = true;
        foreach (string i in segments)
        {
            string safeName = i.Split("?")[0];
            print($"Thread-{whoami}: Downloading {safeName} for VideoFrame ({hash})");
            tries = 0;
            while (true)
            {
                string finalUrl = result.BaseUrl.Split("playlist.m3u8")[0] + TemplateResolver.Resolve(i, result.Query);
                var req = new RestRequest(finalUrl, Method.Get);
                RestResponse resp = client.Get(req);
                if (resp.IsSuccessStatusCode)
                {
                    File.WriteAllBytes(tempdir + "/" + safeName, resp.RawBytes);
                    print($"Thread-{whoami}: Repairing downloaded video...");
                    string name2 = safeName.Replace(".webm", "-repaired.webm");
                    names.Add("file '" + name2 + "'");
                    await system($"{dependDir}ffmpeg.exe -i \"%cd%\\temp\\VideoFrame-{hash}\\{safeName}\" -c copy -bsf:v setts=ts=PTS-STARTPTS \"%cd%\\temp\\VideoFrame-{hash}\\{name2}\" >nul 2>&1");
                    if (!File.Exists(tempdir + "/" + name2))
                    {
                        error($"Thread-{whoami}: Repair failed, no output found.");
                        succeeded = false;
                        break;
                    }
                    File.Delete(tempdir + "/" + safeName);
                    break;
                }
                else
                {
                    tries++;
                    if (tries == 3)
                        break;
                    warn($"Thread-{whoami}: Video download failed, retrying...");
                }
            }
            if (!succeeded)
            {
                error($"Thread-{whoami}: Failed to download a necessary segment to rebuild a VideoFrame.");
                break;
            }
        }
        if (succeeded)
        {
            File.WriteAllLines(tempdir + "/" + "videos.txt", names.ToArray());
            print("Merging VideoFrame segments...");
            if(!Directory.Exists("assets/Videos"))
            {
                Directory.CreateDirectory("assets/Videos");
            }
            await system($"{dependDir}ffmpeg.exe -f concat -safe 0 -i \"%cd%\\temp\\VideoFrame-{hash}\\videos.txt\" -c copy \"%cd%\\assets\\Videos\\{hash}.webm\" -y >nul 2>&1");
        }
        Directory.Delete(tempdir, true);
    }
}

public static class FontList
{
    public static async Task Process(int whoami, string dumpName, byte[] content)
    {
        try
        {
            if(!Directory.Exists("assets/Fonts"))
            {
                Directory.CreateDirectory("assets/Fonts");
            }
            JObject js = JObject.Parse(Encoding.UTF8.GetString(content));
            var outname = js["name"];
            await File.WriteAllBytesAsync($"assets/Fonts/{outname}.json", content);
            JArray faces = (JArray)js["faces"];
            debug($"Thread-{whoami}: Found {faces.Count} fonts");
            foreach (JObject item in faces)
            {
                if(File.Exists($"assets/Fonts/{outname}-{item["name"]}.ttf"))
                {
                    debug($"Thread-{whoami}: Skipping already dumped Font.");
                    continue;
                }
                string asset = item["assetId"].ToString();
                if (asset.Contains("rbxassetid://"))
                {
                    print($"Thread-{whoami}: Downloading {outname}-{item["name"]}.ttf...");
                    var assetid = asset.Split("rbxassetid://")[1];
                    byte[] fontdata = null;
                    int tries = 0;
                    while (true)
                    {
                        var req = new RestRequest("https://assetdelivery.roblox.com/v1/asset?id=" + assetid, Method.Get);
                        RestResponse resp = client.Get(req);
                        if (resp.IsSuccessStatusCode)
                        {
                            fontdata = resp.RawBytes;
                            break;
                        }
                        else
                        {
                            tries++;
                            if (tries == 3)
                                break;
                            warn($"Thread-{whoami}: Font download failed, retrying...");
                        }
                    }
                    if (fontdata != null)
                    {
                        await File.WriteAllBytesAsync($"assets/Fonts/{outname}-{item["name"]}.ttf", fontdata);
                    } else
                    {
                        error($"Thread-{whoami}: Font download failed 3 times, giving up.");
                    }
                }
                else
                {
                    if (asset.Contains("rbxasset://"))
                    {
                        debug($"Thread-{whoami}: Skipping {outname}-{item["name"]}.ttf because it is a local asset.");
                    }
                    else
                    {
                        debug($"Thread-{whoami}: Skipping {outname}-{item["name"]}.ttf because the 'rbxassetid' identifer was not found.");
                    }
                }
            }
        } catch
        {
            debug($"Thread-{whoami}: Something went wrong reading a JSON font list!");
        }
    }
}

public static class Translation
{
    public static async Task Process(int whoami, string dumpName, byte[] content)
    {
        string outDir = $"assets/Translations";
        try
        {
            JObject js = JObject.Parse(Encoding.UTF8.GetString(content));
            if (js.ContainsKey("locale"))
            {
                string locale = (string)js["locale"];
                string path = $"{outDir}/locale-{locale}-{dumpName}.json";
                if (File.Exists(path))
                {
                    debug($"Thread-{whoami}: Skipping already dumped translation.");
                    return;
                }
                print($"Thread-{whoami}: Saving translation locale: {locale}");
                if (!Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);
                await File.WriteAllBytesAsync(path, content);
            }
            else
            {
                warn($"Thread-{whoami}: Locale name not found in translation.");
                await File.WriteAllBytesAsync($"{outDir}/{dumpName}.json", content);
            }
        }
        catch (JsonReaderException)
        {
            warn($"Thread-{whoami}: Failed to decode JSON in translation.");
        }
        catch (Exception ex)
        {
            error($"Thread-{whoami}: Failed to save translation: {ex.Message}");
        }
    }
}

public static class Khronos
{
    public static async Task Process11(int whoami, string dumpName, byte[] content)
    {
        string outDir = $"assets\\KTX Textures";
        if (!Directory.Exists(outDir))
        {
            Directory.CreateDirectory(outDir);
        }
        else
        {
            if (File.Exists($"{outDir}\\{dumpName}.png"))
            {
                debug($"Thread-{whoami}: Skipping already dumped Khronos Texture.");
                return;
            }
        }
        try
        {
            uint glfmt = BitConverter.ToUInt32(content, 28);
            TextureFormat fmt = DetectFormat(glfmt);
            if (fmt == TextureFormat.BCn)
            {
                print($"Thread-{whoami}: Converting Khronos Texture 1.1...");
                MemoryStream ms = new MemoryStream(content);
                KtxFile ktxFile = KtxFile.Load(ms);
                BcDecoder decoder = new BcDecoder();
                Memory2D<ColorRgba32> decodedImage = await decoder.Decode2DAsync(ktxFile);

                var output = new Image<Rgba32>(decodedImage.Width, decodedImage.Height);
                for (var y = 0; y < decodedImage.Height; y++)
                {
                    var yPixels = output.Frames.RootFrame.PixelBuffer.DangerousGetRowSpan(y);
                    var yColors = decodedImage.Span.GetRowSpan(y);

                    MemoryMarshal.Cast<ColorRgba32, Rgba32>(yColors).CopyTo(yPixels);
                }

                output.SaveAsPng($"{outDir}\\{dumpName}.png");
            }
            else if (fmt == TextureFormat.ASTC)
            {
                print($"Thread-{whoami}: Converting Khronos Texture 1.1...");
                var tex = KTX1.KtxRipper.KtxTexture.OpenFromMemory(content);
                tex.SaveToPng($"{outDir}\\{dumpName}.png");
            }
            else
            {
                warn($"Thread-{whoami}: Unsupported Khronos Texture 1.1 format! (0x{glfmt.ToString("X")} - {fmt})");
            }
        }
        catch (Exception ex)
        {
            error($"Thread-{whoami}: Error converting Khronos Texture 1.1! (" + ex.Message + ")");
            return;
        }
    }

    public static async Task Process20(int whoami, string dumpName, byte[] content)
    {
        string outDir = $"assets\\KTX Textures";
        if (!Directory.Exists(outDir))
        {
            Directory.CreateDirectory(outDir);
        }
        else
        {
            if (File.Exists($"{outDir}\\{dumpName}.png"))
            {
                debug($"Thread-{whoami}: Skipping already dumped Khronos Texture. {exedir}");
                return;
            }
        }
        try
        {
            unsafe
            {
                KtxTexture* texture = Ktx.LoadFromMemory(content);
                if(texture == null)
                {
                    warn($"Thread-{whoami}: Failed to load Khronos Texture 2.0!");
                    return;
                }
                if (NeedsTranscoding(texture))
                {
                    KtxErrorCode err = Transcode(texture, TranscodeFormat.Bc3Rgba, TranscodeFlagBits.HighQuality);
                    if (err != KtxErrorCode.KtxSuccess)
                    {
                        warn($"Thread-{whoami}: Failed to transcode Khronos Texture 2.0! ({err})");
                        Destroy(texture);
                        return;
                    }
                }

                if((int)texture->VulkanFormat <= 130 || (int)texture->VulkanFormat >= 147)
                {
                    warn($"Thread-{whoami}: Unsupported Khronos Texture 2.0 format: {texture->VulkanFormat}");
                    Destroy(texture);
                    return;
                }

                print($"Thread-{whoami}: Converting Khronos Texture 2.0...");

                CompressionFormat fmt = ConvertFormat(texture->VulkanFormat);
                int blockSize = GetBlockSize(texture->VulkanFormat);
                uint width = texture->BaseWidth;
                uint height = texture->BaseHeight;
                uint offset = GetImageOffset(texture, 0, 0, 0);
                byte* basePtr = (byte*)texture->Data + offset;

                int blocksWide = (int)((width + 3) / 4);
                int blocksHigh = (int)((height + 3) / 4);
                int dataSize = blocksWide * blocksHigh * blockSize;

                byte[] texData = new byte[dataSize];
                Marshal.Copy((IntPtr)basePtr, texData, 0, texData.Length);
                Destroy(texture);

                BcDecoder decoder = new BcDecoder();
                Memory2D<ColorRgba32> colors = decoder.DecodeRaw2D(texData, (int)width, (int)height, fmt);
                var output = new Image<Rgba32>(colors.Width, colors.Height);
                for (var y = 0; y < colors.Height; y++)
                {
                    var yPixels = output.Frames.RootFrame.PixelBuffer.DangerousGetRowSpan(y);
                    var yColors = colors.Span.GetRowSpan(y);

                    MemoryMarshal.Cast<ColorRgba32, Rgba32>(yColors).CopyTo(yPixels);
                }
                output.SaveAsPng($"{outDir}\\{dumpName}.png");
            }
        }
        catch (Exception ex)
        {
            error($"Thread-{whoami}: Error converting Khronos Texture 2.0! (" + ex.Message + ")");
            return;
        }
    }
}
