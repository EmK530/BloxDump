#pragma warning disable CS8600,CS8602

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;
using RestSharp;

using static Essentials;

public static class EXTM3U
{
    public static async Task Process(int whoami, string dumpName, byte[] cont)
    {
        if(File.Exists($"assets/Videos/{dumpName}.webm"))
        {
            debug($"Thread-{whoami}: Skipping already dumped VideoFrame.");
            return;
        }
        string content = Encoding.UTF8.GetString(cont);
        if (!content.Contains("RBX-BASE-URI"))
        {
            debug($"Thread-{whoami}: Ignoring undesired EXTM3U file.");
            return;
        }
        print($"Thread-{whoami}: Dumping asset type: VideoFrame");
        (string, string, string) M3Data = ParseEXTM3U(content);
        if (M3Data.Item1 == "")
        {
            error($"Thread-{whoami}: No stream URL found for VideoFrame.");
            return;
        }
        print($"Thread-{whoami}: Downloading metadata for best resolution: " + M3Data.Item2);
        string metadata = "";
        int tries = 0;
        while (true)
        {
            var req = new RestRequest(M3Data.Item1, Method.Get);
            RestResponse resp = client.Get(req);
            if (resp.IsSuccessStatusCode)
            {
                metadata = resp.Content;
                break;
            }
            else
            {
                tries++;
                if (tries == 3)
                    break;
                warn($"Thread-{whoami}: Metadata download failed, retrying...");
            }
        }
        if(metadata=="")
        {
            error($"Thread-{whoami}: Metadata download failed 3 times, giving up.");
            return;
        }
        string hash = dumpName; //string hash = M3Data.Item3.Split("/").Last();
        string file = M3Data.Item1.Split("/").Last();
        string segmentUrl = M3Data.Item1.Replace(file, "");
        List<string> segments = new List<string>();
        bool nextIsASegment = false;
        foreach (string i in metadata.Split("\n"))
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
        List<string> names = new List<string>();
        bool succeeded = true;
        foreach (string i in segments)
        {
            print($"Thread-{whoami}: Downloading {i} for VideoFrame ({hash})");
            tries = 0;
            while (true)
            {
                var req = new RestRequest(segmentUrl + i, Method.Get);
                RestResponse resp = client.Get(req);
                if (resp.IsSuccessStatusCode)
                {
                    File.WriteAllBytes(tempdir + "/" + i, resp.RawBytes);
                    print($"Thread-{whoami}: Repairing downloaded video...");
                    string name2 = i.Replace(".webm", "-repaired.webm");
                    names.Add("file '" + name2 + "'");
                    await system($"%temp%\\BloxDump\\ffmpeg.exe -i \"%cd%\\temp\\VideoFrame-{hash}\\{i}\" -c copy -bsf:v setts=ts=PTS-STARTPTS \"%cd%\\temp\\VideoFrame-{hash}\\{name2}\" >nul 2>&1");
                    if (!File.Exists(tempdir + "/" + name2))
                    {
                        error($"Thread-{whoami}: Repair failed, no output found.");
                        succeeded = false;
                        break;
                    }
                    File.Delete(tempdir + "/" + i);
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
            await system($"%temp%\\BloxDump\\ffmpeg.exe -f concat -safe 0 -i \"%cd%\\temp\\VideoFrame-{hash}\\videos.txt\" -c copy \"%cd%\\assets\\Videos\\{hash}.webm\" -y >nul 2>&1");
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
    public static async Task Process(int whoami, string dumpName, byte[] content)
    {
        string outDir = $"assets\\KTX Textures";
        if (!Directory.Exists("temp"))
        {
            Directory.CreateDirectory("temp");
        }
        if (!File.Exists(dependDir+"PVRTexToolCLI.exe"))
        {
            error($"Thread-{whoami}: PVRTexToolCLI does not exist, cannot convert Khronos Texture.");
            return;
        }
        if (File.Exists($"{outDir}\\{dumpName}.png"))
        {
            debug($"Thread-{whoami}: Skipping already dumped Khronos Texture.");
            return;
        }
        print($"Thread-{whoami}: Converting Khronos Texture...");
        await File.WriteAllBytesAsync($"temp/{dumpName}.ktx", content);
        if (!Directory.Exists(outDir))
        {
            Directory.CreateDirectory(outDir);
        }
        await system($"%temp%\\BloxDump\\PVRTexToolCLI.exe -i \"%cd%\\temp\\{dumpName}.ktx\" -noout -shh -d \"%cd%\\{outDir}\\{dumpName}.png\"");
        srgb2lin.convert($"{outDir}\\{dumpName}.png");
        File.Delete($"temp/{dumpName}.ktx");
    }
}