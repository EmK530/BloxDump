using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RestSharp;
using System.Diagnostics;
using System.Text;

class Essentials
{
    public static string app_name = "BloxDump";
    public static string app_version = "v5.2.7";

    private static bool usingFallbackConfig = true;

    private static long current_cfg_ver = 2; // increment with new keys
    private static Dictionary<string, object> config = new Dictionary<string, object>()
    {
        ["cfg_ver"] = current_cfg_ver,
        ["DebugLogging"] = false,
        ["DependencyDir"] = "{TEMP}BloxDump\\",
        ["DumperSettings"] = new Dictionary<string, object>()
        {
            ["BlockAvatarImages"] = true,
            ["CustomThreadCount"] = new Dictionary<string, object>()
            {
                ["Enable"] = false,
                ["Target"] = 16
            }
        },
        ["Cache"] = new Dictionary<string, object>()
        {
            ["PromptClearOnLaunch"] = true,
            ["AutoClearIfNoPrompt"] = false,
            ["WebClient"] = new Dictionary<string, object>()
            {
                ["Path"] = "{LOCALAPPDATA}Roblox\\rbx-storage.db",
                ["IsDatabase"] = true,
                ["DBFolder"] = "{LOCALAPPDATA}Roblox\\rbx-storage\\"
            },
            ["UWPClient"] = new Dictionary<string, object>()
            {
                ["Path"] = "{LOCALAPPDATA}Packages\\ROBLOXCORPORATION.ROBLOX_55nm5eh3cm0pr\\LocalState\\http\\",
                ["IsDatabase"] = false,
                ["DBFolder"] = "N/A"
            },
            ["ForceCustomDirectory"] = new Dictionary<string, object>()
            {
                ["Enable"] = false,
                ["TargetDirectory"] = "{TEMP}Roblox\\http\\",
                ["IsDatabase"] = false,
                ["DBFolder"] = "N/A"
            }
        },
        ["Aliases"] = new Dictionary<string, object>()
        {
            ["TempPath"] = "{TEMP}",
            ["LocalAppData"] = "{LOCALAPPDATA}"
        }
    };
    private static Dictionary<string, object> fallbackConfig = DeepClone(config);

    private static bool firstRun = true;
    private static bool doNotLoad = false;

    public static string tempDir = Path.GetTempPath() + "\\";
    public static string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\";

    private static Dictionary<string, string> aliasDict = new Dictionary<string, string>()
    {
        ["TempPath"] = tempDir,
        ["LocalAppData"] = localAppData
    };

    public static bool EnableDebug = ReadConfigBoolean("DebugLogging");
    public static bool BlockAvatarImages = ReadConfigBoolean("DumperSettings.BlockAvatarImages");
    public static string dependDir = ReadAliasedString("DependencyDir");

    public static string webPath = ReadAliasedString("Cache.WebClient.Path");
    public static bool webIsDatabase = ReadConfigBoolean("Cache.WebClient.IsDatabase");
    public static string webDB = ReadAliasedString("Cache.WebClient.DBFolder");
    public static string UWPPath = ReadAliasedString("Cache.UWPClient.Path");
    public static bool UWPisDatabase = ReadConfigBoolean("Cache.UWPClient.IsDatabase");
    public static string UWPdb = ReadAliasedString("Cache.WebClient.DBFolder");

    public static void debug(string input)
    {
#if DEBUG
        Console.WriteLine("\x1b[6;30;44m" + "DEBUG" + "\x1b[0m " + input);
#else
        if(EnableDebug)
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
            string? exedir = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
            if (string.IsNullOrWhiteSpace(exedir))
            {
                exedir = AppContext.BaseDirectory;
            }
            return exedir;
        }
    }

    private static Dictionary<string, object> DeepClone(Dictionary<string, object> original)
    {
        var clone = new Dictionary<string, object>();

        foreach (var kvp in original)
        {
            if (kvp.Value is Dictionary<string, object> nestedDict)
            {
                clone[kvp.Key] = DeepClone(nestedDict);
            }
            else
            {
                clone[kvp.Key] = kvp.Value;
            }
        }

        return clone;
    }

    private static bool SerializeConfig()
    {
        string json = JsonConvert.SerializeObject(config, Formatting.Indented);
        try
        {
            File.WriteAllText("config.json", json);
            return true;
        }
        catch (Exception ex)
        {
            error($"Error creating config: {ex.Message}\n{ex.StackTrace}");
        }
        return false;
    }

    public static bool LoadConfig()
    {
        if (firstRun)
        {
            systemSync("cls");
            Console.Clear();
            firstRun = false;
        }
        if (doNotLoad)
            return false;
        print("Attempting config load...");
        if (!File.Exists("config.json"))
        {
            warn("config.json does not exist! Creating new file from fallback!");
            if (SerializeConfig())
                LoadConfig();
            return false;
        }
        try
        {
            string json = File.ReadAllText("config.json");
            JToken root = JToken.Parse(json);
            var cfg = ConvertJTokenToDictionary(root);
            object? cfg_ver = ReadConfigObject("cfg_ver", cfg);
            if (cfg_ver == null || cfg_ver.GetType() != typeof(long) || (long)cfg_ver < current_cfg_ver)
            {
                warn("Config is invalid or outdated, overwriting...");
                SerializeConfig();
            }
            else
            {
                config = cfg;
            }
            usingFallbackConfig = false;
            print("Successfully loaded config!");
            return true;
        }
        catch (Exception ex)
        {
            doNotLoad = true;
            error($"Error loading config: {ex.Message}");
            return false;
        }
    }

    private static Dictionary<string, object> ConvertJTokenToDictionary(JToken token)
    {
        if (token is JObject jObject)
        {
            return jObject.Properties()
                .ToDictionary(
                    prop => prop.Name,
                    prop => UnwrapJToken(prop.Value)
                );
        }
        throw new ArgumentException("Token must be a JSON object");
    }

    private static object UnwrapJToken(object value)
    {
        return value switch
        {
            JValue jValue => jValue.Value,
            JObject jObject => ConvertJTokenToDictionary(jObject),
            JArray jArray => jArray.Select(UnwrapJToken).ToList(),
            _ => null
        };
    }

    public static object? ReadConfigObject(string key, Dictionary<string, object>? cfg = null, bool recurse = false)
    {
        if (usingFallbackConfig && cfg == null && !LoadConfig() && !recurse)
            warn($"Using fallback config for '{key}' because config failed to load.");
        string[] parts = key.Split('.');
        object? target = (cfg == null ? (usingFallbackConfig ? fallbackConfig : config) : cfg);
        foreach (string part in parts)
        {
            if (target is Dictionary<string, object> dict)
            {
                if (!dict.TryGetValue(part, out target))
                {
                    warn($"Cannot read config '{part}' in chain '{key}' because the parent contains no such key.");
                    return recurse ? null : (key != "cfg_ver" ? ReadConfigObject(key, fallbackConfig, true) : null);
                }
            }
            else
            {
                warn($"Cannot read config '{part}' in chain '{key}' because the parent is not a dictionary.");
                warn(target == null);
                warn(fallbackConfig == null);
                return recurse ? null : ReadConfigObject(key, fallbackConfig, true);
            }
        }
        return target;
    }

    public static bool ReadConfigBoolean(string key)
    {
        object? target = ReadConfigObject(key);
        if (target is bool result)
            return result;
        if (target != null)
            warn($"The config value for '{key}' is not a boolean!");
        return false;
    }

    public static int ReadConfigInteger(string key)
    {
        object? target = ReadConfigObject(key);
        if (target is int result)
            return result;
        if (target != null)
            warn($"The config value for '{key}' is not an integer!");
        return 0;
    }

    public static string ReadConfigString(string key)
    {
        object? target = ReadConfigObject(key);
        if (target is string result)
            return result;
        if (target != null)
            warn($"The config value for '{key}' is not a string!");
        return "<value_not_found>";
    }

    public static string ReadAliasedString(string key)
    {
        object? target = ReadConfigObject(key);
        if (target is string result)
        {
            object? aliases = ReadConfigObject("Aliases");
            if (aliases is Dictionary<string, object> dict)
            {
                foreach (KeyValuePair<string, object> kvp in dict)
                {
                    if (!aliasDict.ContainsKey(kvp.Key))
                    {
                        warn($"Ignoring invalid alias key '{kvp.Key}'");
                        continue;
                    }
                    if (kvp.Value is string str)
                    {
                        string replacement = aliasDict[kvp.Key];
                        string toReplace = (string)kvp.Value;
                        result = result.Replace(toReplace, replacement);
                    }
                    else
                    {
                        warn($"Ignoring non-string alias key '{kvp.Key}'");
                        continue;
                    }
                }
            }
            else
            {
                warn($"Cannot rewrite aliases for config '{key}' because aliases didn't exist or isn't a dictionary!");
            }
            return result;
        }
        if (target != null)
            warn($"The config value for '{key}' is not a string!");
        return "<value_not_found>";
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
        }
        catch (Exception ex)
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
        if (content == null)
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
            if (content == null)
            {
                this.content = new byte[0];
            }
            else
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

    public static bool EmptyFolder()
    {
        try
        {
            string path = CacheScanner.targetPath;
            if (CacheScanner.TargetIsDatabase)
            {
                File.Delete(path);
                path = CacheScanner.dbFolder;
            }
            foreach (string file in Directory.GetFiles(path))
            {
                File.Delete(file);
            }
            foreach (string dir in Directory.GetDirectories(path))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch (Exception)
        {
            return false;
        }
        return true;
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
            var s when s.Contains("KTX 11") => (AssetType.Khronos, "", "", ""),
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

    public enum TextureFormat
    {
        Unknown,
        Uncompressed,
        BCn,
        ASTC
    }

    public static TextureFormat DetectFormat(uint internalFormat)
    {
        if ((internalFormat >= 0x1900 && internalFormat <= 0x1908) ||
            (internalFormat & 0xFF00) == 0x8200)
        {
            return TextureFormat.Uncompressed;
        }
        else if ((internalFormat & 0xFF00) == 0x8300 ||
                 (internalFormat & 0xFF00) == 0x8D00 ||
                 (internalFormat & 0xFF00) == 0x8E00)
        {
            return TextureFormat.BCn;
        }
        else if ((internalFormat & 0xFF00) == 0x9200 ||
                 (internalFormat & 0xFF00) == 0x9300)
        {
            return TextureFormat.ASTC;
        }
        return TextureFormat.Unknown;
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

    public static void systemSync(string cmd)
    {
        Process process = new Process();
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        startInfo.FileName = "cmd.exe";
        startInfo.Arguments = "/C " + cmd;
        startInfo.WorkingDirectory = exedir;
        process.StartInfo = startInfo;
        process.Start();
        process.WaitForExit();
    }

    public static ParsedCache ParseCache(Dumper.Cache asset)
    {
        if(asset.Data != null)
        {
            MemoryStream str = new MemoryStream(asset.Data);
            BinaryReader br = new BinaryReader(str);
            return ParseCache(br);
        }
        if (!File.Exists(asset.Path))
        {
            warn("Cache path not found: " + asset.Path);
            return new ParsedCache(false);
        }
        Stream data = File.OpenRead(asset.Path);
        BinaryReader rd = new BinaryReader(data);
        return ParseCache(rd);
    }

    public static ParsedCache ParseCache(BinaryReader rd)
    {
        Encoding utf = Encoding.UTF8;

        string magic = utf.GetString(rd.ReadBytes(4));
        if (magic != "RBXH")
        {
            debug("Ignoring non-RBXH magic: " + magic);
            return new ParsedCache(false);
        }
        rd.BaseStream.Position += 4; // skip header size
        uint linklen = rd.ReadUInt32();
        string link = utf.GetString(rd.ReadBytes((int)linklen));
        if (knownlinks.Contains(link))
        {
            //debug("Ignoring duplicate CDN link: " + link);
            return new ParsedCache(false);
        }
        rd.BaseStream.Position++; // rogue byte xdd

        uint status = rd.ReadUInt32();
        if (status >= 300)
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

        return new ParsedCache(true, link, content);
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
