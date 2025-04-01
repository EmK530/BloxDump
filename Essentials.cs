using RestSharp;
using System.Diagnostics;
using System.Text;

class Essentials
{
    public static string app_name = "BloxDump";
    public static string app_version = "v5.2.1";

    public static bool BlockAvatarImages = true;
    public static bool PromptCacheClear = true;
    public static bool AutoClear = false;
    public static bool debugMode = false;

    public static string tempDir = Path.GetTempPath();
    public static string dependDir = tempDir + "BloxDump\\";
    public static string webPath = tempDir + "Roblox\\http\\";
    public static string UWPPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
        "\\Packages\\ROBLOXCORPORATION.ROBLOX_55nm5eh3cm0pr\\LocalState\\http\\";

    public static void debug(string input) {
#if DEBUG
        Console.WriteLine("\x1b[6;30;44m" + "DEBUG" + "\x1b[0m " + input);
#else
        if(debugMode)
            Console.WriteLine("\x1b[6;30;44m" + "DEBUG" + "\x1b[0m " + input);
#endif
    }
    public static void print(object input) { Console.WriteLine("\x1b[6;30;47m" + "INFO" + $"\x1b[0m {input}"); }
    public static void warn(object input) { Console.WriteLine("\x1b[6;30;43m" + "WARN" + $"\x1b[0m {input}"); }
    public static void error(object input) { Console.WriteLine("\x1b[6;30;41m" + "ERROR" + $"\x1b[0m {input}"); }
    public static void fatal(object input) { Console.WriteLine("\x1b[6;30;41m" + "FATAL" + $"\x1b[0m {input}"); Thread.Sleep(5000); Environment.Exit(0); }

    public static string exedir
    {
        get
        {
            string? exedir = Path.GetDirectoryName(AppContext.BaseDirectory);
            if (string.IsNullOrWhiteSpace(exedir))
            {
                exedir = AppDomain.CurrentDomain.BaseDirectory;
            }
            return exedir;
        }
    }

    public static async Task<byte[]?> Download(HttpClient client, string URL)
    {
        try
        {
            HttpResponseMessage resp = await client.GetAsync(URL);
            byte[] content = await resp.Content.ReadAsByteArrayAsync();
            if (!resp.IsSuccessStatusCode)
            {
                warn("Non-success downloading asset:");
                warn(Encoding.UTF8.GetString(content));
                return null;
            }
            return content;
        } catch(Exception ex)
        {
            error("Error downloading asset:");
            error(ex);
            return null;
        }
    }

    public static async Task<bool> DownloadDependencyAsync(string filename, string url)
    {
        print("Downloading dependency: " + filename);
        byte[]? content = await Download(new HttpClient(), url);
        if(content == null)
        {
            return false;
        }
        await File.WriteAllBytesAsync(dependDir + filename, content);
        return true;
    }

    public struct ParsedCache
    {
        public ParsedCache(bool success, string link = "", byte[]? content = null)
        {
            this.success = success;
            this.link = link;
            if(content == null)
            {
                this.content = new byte[0];
            } else
            {
                this.content = content;
            }
        }
        public bool success = false;
        public string link;
        public byte[] content;
    }

    private static List<string> knownlinks = new List<string>();

    public enum AssetType
    {
        Unknown = 0,
        Ignored = 1,
        NoConvert = 2,
        Mesh = 3,
        Khronos = 4,
        EXTM3U = 5,
        Translation = 6,
        FontList = 7,
        WebP = 8
    }

    public static RestClient client = new RestClient();

    public static (AssetType, string, string, string) IdentifyContent(byte[] cnt)
    {
        string begin = Encoding.UTF8.GetString(cnt[..Math.Min(48, cnt.Length - 1)]);
        uint magic = BitConverter.ToUInt32(cnt, 0);
        return begin switch
        {
            var s when s.Contains("<roblox!") => (AssetType.NoConvert, "rbxm", "RBXM", "RBXM"),
            var s when s.Contains("<roblox xml") => (AssetType.Ignored, "", "unsupported XML", ""),
            var s when !s.Contains("\"version") && s.StartsWith("version") => (AssetType.Mesh, "", "", ""),
            var s when s.StartsWith("{\"translations") => (AssetType.Ignored, "", "translation list JSON", ""),
            var s when s.Contains("{\"locale\":\"") => (AssetType.Translation, "", "", ""),
            var s when s.Contains("PNG\r\n") => (AssetType.NoConvert, "png", "PNG", "Textures"),
            var s when s.StartsWith("GIF87a") || s.StartsWith("GIF89a") => (AssetType.NoConvert, "gif", "GIF", "Textures"),
            var s when s.Contains("JFIF") || s.Contains("Exif") => (AssetType.NoConvert, "jfif", "JFIF", "Textures"),
            var s when s.StartsWith("RIFF") && s.Contains("WEBP") => (BlockAvatarImages ? (AssetType.WebP, "webp", "WebP", "Textures") : (AssetType.NoConvert, "webp", "WebP", "Textures")),
            var s when s.StartsWith("OggS") => (AssetType.NoConvert, "ogg", "OGG", "Sounds"),
            var s when s.StartsWith("ID3") || (cnt.Length > 2 && (cnt[0] & 0xFF) == 0xFF && (cnt[1] & 0xE0) == 0xE0) => (AssetType.NoConvert, "mp3", "MP3", "Sounds"),
            var s when s.Contains("KTX ") => (AssetType.Khronos, "", "", ""),
            var s when s.StartsWith("#EXTM3U") => (AssetType.EXTM3U, "", "", ""),
            var s when s.Contains("\"name\": \"") => (AssetType.FontList, "", "", ""),
            var s when s.Contains("{\"applicationSettings") => (AssetType.Ignored, "", "FFlags JSON", ""),
            var s when s.Contains("{\"version") => (AssetType.Ignored, "", "client version JSON", ""),
            var s when s.Contains("GDEF") || s.Contains("GPOS") || s.Contains("GSUB") => (AssetType.Ignored, "", "OpenType/TrueType font", ""),
            var s when magic == 0xFD2FB528 => (AssetType.Ignored, "", "Zstandard compressed data (likely FFlags)", ""),
            var s when cnt.Length >= 4 && cnt[0] == 0x1A && cnt[1] == 0x45 && cnt[2] == 0xDF && cnt[3] == 0xA3 => (AssetType.Ignored, "", "VideoFrame segment", ""),
            _ => (AssetType.Unknown, begin, "", "")
        };
    }

    public static async Task system(string cmd)
    {
        Process process = new Process();
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        startInfo.FileName = "cmd.exe";
        startInfo.Arguments = "/C " + cmd;
        startInfo.WorkingDirectory = exedir;
        process.StartInfo = startInfo;
        process.Start();
        await process.WaitForExitAsync();
    }

    public static ParsedCache ParseCache(string path)
    {
        if (!File.Exists(path))
        {
            warn("Cache path not found: " + path);
            return new ParsedCache(false);
        }
        Stream data = File.OpenRead(path);
        BinaryReader rd = new BinaryReader(data);
        Encoding utf = Encoding.UTF8;

        string magic = utf.GetString(rd.ReadBytes(4));
        if(magic!="RBXH")
        {
            debug("Ignoring non-RBXH magic: " + magic);
            return new ParsedCache(false);
        }
        rd.BaseStream.Position += 4; // skip header size
        uint linklen = rd.ReadUInt32();
        string link = utf.GetString(rd.ReadBytes((int)linklen));
        if(knownlinks.Contains(link))
        {
            //debug("Ignoring duplicate CDN link: " + link);
            return new ParsedCache(false);
        }
        rd.BaseStream.Position++; // rogue byte xdd

        uint status = rd.ReadUInt32();
        if(status >= 300)
        {
            //debug("Ignoring non-successful cache.");
            return new ParsedCache(false);
        }
        uint headerlen = rd.ReadUInt32();
        rd.BaseStream.Position += 4; // skip XXHash digest
        uint contentlen = rd.ReadUInt32();
        rd.BaseStream.Position += 8 + headerlen; // skip XXHash digest, reserved bytes and headers
        byte[] content = rd.ReadBytes((int)contentlen);
        knownlinks.Add(link);

        rd.Dispose();
        data.Dispose();

        return new ParsedCache(true,link,content);
    }

    public static (string, string, string) ParseEXTM3U(string content)
    {
        string[] lines = content.Split("\n");
        string url = "";
        string beststream = "";
        string bestRes = "";
        double beststreamBW = 0d;
        bool writeNextLine = false;
        foreach (string i in lines)
        {
            if (i.StartsWith("#"))
            {
                if (i != "#EXTM3U" && i.Contains(":"))
                {
                    string[] extsplit = i.Split(":", 2);
                    string identifier = extsplit[0];
                    string config = extsplit[1];
                    if (identifier == "#EXT-X-DEFINE")
                    {
                        Dictionary<string, object> cfgd = EXTStringToDict(config);
                        if ((string)cfgd["NAME"] == "RBX-BASE-URI")
                        {
                            url = (string)cfgd["VALUE"];
                        }
                    }
                    else if (identifier == "#EXT-X-STREAM-INF")
                    {
                        Dictionary<string, object> cfgd = EXTStringToDict(config);
                        double bw = (double)cfgd["BANDWIDTH"];
                        if (bw > beststreamBW)
                        {
                            beststreamBW = bw;
                            bestRes = (string)cfgd["RESOLUTION"];
                            writeNextLine = true;
                        }
                    }
                }
            }
            else if (writeNextLine)
            {
                writeNextLine = false;
                beststream = i.Replace("{$RBX-BASE-URI}", url);
            }
        }
        return (beststream, bestRes, url);
    }

    private static Dictionary<string, object> EXTStringToDict(string input)
    {
        Dictionary<string, object> output = new Dictionary<string, object>();
        string[] items = input.Split(",");
        foreach (string i in items)
        {
            if (i.Contains("="))
            {
                string[] spl2 = i.Split("=");
                string tryParse = spl2[1].Replace("\"", "");
                if (double.TryParse(tryParse, out double conv))
                {
                    output.Add(spl2[0], conv);
                }
                else
                {
                    output.Add(spl2[0], tryParse);
                }
            }
        }
        return output;
    }
}
