using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using RestSharp;

#pragma warning disable CS8600
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8604

bool db = false;

string client_name = "BloxDump v5.1.0" + (db ? " (debug)" : "");

void debug(string input) { if (db) { Console.WriteLine("\x1b[6;30;44m" + "DEBUG" + "\x1b[0m " + input); } }
void print(string input) { Console.WriteLine("\x1b[6;30;47m" + "INFO" + "\x1b[0m " + input); }
void warn(string input) { Console.WriteLine("\x1b[6;30;43m" + "WARN" + "\x1b[0m " + input); }
void error(string input) { Console.WriteLine("\x1b[6;30;41m" + "ERROR" + "\x1b[0m " + input); }

string exedir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
if (string.IsNullOrWhiteSpace(exedir))
{
    exedir = AppDomain.CurrentDomain.BaseDirectory;
}
string curpath = exedir + "\\";

int max_threads = Environment.ProcessorCount;
bool manualThreadCount = false;
bool promptCacheClear = true;
bool autoClear = false;

List<Thread> threads = new List<Thread>();
object lockObject = new object();

if(File.Exists("config.json"))
{
    JObject config = JObject.Parse(File.ReadAllText("config.json"));
    manualThreadCount = (bool)config["promptThreadCount"];
    promptCacheClear = (bool)config["promptCacheClear"];
    autoClear = (bool)config["noprompt_autoClearCache"];
}
else
{
    File.WriteAllText("config.json", """
        {
            "promptThreadCount": false,
            "promptCacheClear": true,
            "noprompt_autoClearCache": false
        }
        """);
}

void check_thread_life()
{
    List<Thread> stillAlive = new List<Thread>();
    lock (lockObject)
    {
        foreach (Thread thread in threads)
        {
            if (thread.IsAlive)
            {
                stillAlive.Add(thread);
            }
        }
        threads = stillAlive;
    }
}

void system(string cmd)
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

string webPath = Path.GetTempPath() + "Roblox\\http\\";
string UWPPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + 
    "\\Packages\\ROBLOXCORPORATION.ROBLOX_55nm5eh3cm0pr\\LocalState\\http\\";

string usePath = webPath; // might change later down

Console.Clear();
system("cls");
//those clears ensure that the print labels work

bool isDir1 = Directory.Exists(webPath);
bool isDir2 = Directory.Exists(UWPPath);

bool panic = false;

Console.Title = client_name + " | Setup";

if (isDir1 && isDir2)
{
    Dictionary<string, string> paths = new Dictionary<string, string>()
    {
        ["Standard Version"] = webPath,
        ["UWP Version (Microsoft Store)"] = UWPPath
    };

    Console.WriteLine("Which version of Roblox do you use?");
    Console.WriteLine("If your input is incorrect then dumping will not work as intended.\n");
    Console.WriteLine($"1: Standard Version\n2: UWP Version (Microsoft Store)");
    bool done = false;
    while (!done)
    {
        Console.Write("\nInput: ");
        string get = Console.ReadLine();
        if (int.TryParse(get, out int parsed))
        {
            switch(parsed)
            {
                case 1:
                    usePath = webPath;
                    done = true;
                    break;
                case 2:
                    usePath = UWPPath;
                    done = true;
                    break;
                default:
                    warn($"Unrecognized choice: {parsed}");
                    break;
            }
        }
    }
}
else if(isDir2 && !isDir1)
{
    usePath = UWPPath;
} else if(!isDir1 && !isDir2)
{
    // no temp path exists, panic
    panic = true;
}

system("cls");

List<string> known = new List<string>();
List<string> knownlinks = new List<string>();
string[] bans =
{
    "noFilter",
    "Png",
    "isCircular",
    "3319998a6720cf9d1a879e6e7ed25f52"
};

var client = new RestClient();
client.AddDefaultHeaders(new Dictionary<string, string>()
{
    ["User-Agent"] = "BloxDump",
    ["Accept"] = "binary/octet-stream",
    ["Accept-Encoding"] = "gzip, deflate"
});

byte[] DownloadFile(string link)
{
    byte[] cont = null;
    debug("Downloading asset from " + link);
    int retryCount = 0;
    while (retryCount < 3) // Set a limit for retry attempts
    {
        bool success = false;
        string ex = "";
        RestResponse resp = new RestResponse();
        try
        {
            var req = new RestRequest(link, Method.Get);
            resp = client.Get(req);
            success = true;
        }
        catch (Exception exc)
        {
            ex = exc.Message;
        }
        if (success && resp.IsSuccessStatusCode)
        {
            cont = resp.RawBytes;
            break;
        }
        else
        {
            retryCount++;
            if (!success)
            {
                error("Asset download failed with exception '" + ex + "', retrying attempt " + retryCount + "...");
            }
            else
            {
                warn("Asset download failed, retrying attempt " + retryCount + "...");
            }
            Thread.Sleep(100);
        }
    }
    if (retryCount == 3)
    {
        error("Failed to download asset after 3 attempts, skipping file...");
        return null;
    }
    return cont;
}

(string, string, string) ParseEXTM3U(string content)
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

Dictionary<string, object> EXTStringToDict(string input)
{
    Dictionary<string, object> output = new Dictionary<string, object>();
    string[] items = input.Split(",");
    foreach(string i in items)
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

void thread(string name)
{
    byte[] data = File.ReadAllBytes(name);
    string[] split = name.Split("\\");
    string filename = split[split.Length-1];
    IEnumerator<byte> e = ((IEnumerable<byte>)data).GetEnumerator();
    int enumPos = 0;
    byte ReadByte()
    {
        enumPos++;
        e.MoveNext();
        return e.Current;
    }
    void Skip(int amt)
    {
        enumPos += amt;
        for (int i = 0; i < amt; i++)
        {
            e.MoveNext();
        }
    }
    bool lEndian = true;
    byte[] ReadBytes(uint amt)
    {
        byte[] ret = new byte[amt];
        for (int i = 0; i < amt; i++)
        {
            ret[i] = ReadByte();
        }
        return ret;
    }
    uint ReadUInt32()
    {
        uint sum = 0;
        for (int i = 0; i < 4; i++)
        {
            sum += (uint)(ReadByte() << (lEndian ? (8 * i) : (24 - 8 * i)));
        }
        return sum;
    }
    string ReadString(int len)
    {
        if (len != -1)
        {
            byte[] total = new byte[len];
            for (int i = 0; i < len; i++)
            {
                total[i] = ReadByte();
            }
            return Encoding.UTF8.GetString(total);
        }
        else
        {
            byte begin = ReadByte();
            List<byte> total = new List<byte>() { begin };
            if (begin == 0x00) return "";
            while (true)
            {
                byte check = ReadByte();
                if (check == 0x00)
                {
                    break;
                }
                else
                {
                    total.Add(check);
                }
            }
            return Encoding.UTF8.GetString(total.ToArray());
        }
    }
    string ident = ReadString(4);
    if(ident != "RBXH")
    {
        debug("Ignoring non-RBXH identifier: " + ident);
        return;
    }
    Skip(4);
    uint linklen = ReadUInt32();
    string link = ReadString((int)linklen);
    Skip(1);
    uint reqStatusCode = ReadUInt32();
    string[] s = link.Split("/");
    string outhash = s[s.Length - 1];
    if (bans.Contains(outhash))
    {
        debug("Ignoring blocked hash.");
        return;
    }
    if (knownlinks.Contains(link))
    {
        debug("Ignoring duplicate cdn link.");
        return;
    }
    knownlinks.Add(link);
    byte[] cont;
    if (reqStatusCode == 200)
    {
        debug("Extracting cached data from: " + name);
        uint headerDataLen = ReadUInt32();
        Skip(4);
        uint fileSize = ReadUInt32();
        Skip(8 + (int)headerDataLen);
        cont = ReadBytes(fileSize);
    }
    else
    {
        cont = DownloadFile(link);
    }
    if(cont == null)
    {
        return;
    }
    string begin = Encoding.UTF8.GetString(cont[..Math.Min(48,cont.Length-1)]);
    string output = null;
    string folder = null;
    if (begin.Contains("<roblox!")) { print("Dumping asset type: RBXM Animation"); output = "rbxm"; folder = "Animations"; }
    else if (begin.Contains("<roblox xml")) { debug("Ignoring unsupported XML file."); return; }
    else if (!begin.Contains("\"version") && begin.Contains("version")) { print("Dumping asset type: Roblox Mesh"); output = "mesh"; folder = "Meshes"; }
    else if (begin.Contains("{\"locale\":\"")) { print("Dumping asset type: JSON translation"); output = "translation"; folder = "Translations"; }
    else if (begin.Contains("PNG\r\n")) { print("Dumping asset type: PNG"); output = "png"; folder = "Textures"; }
    else if (begin.StartsWith("GIF8")) { print("Dumping asset type: GIF"); output = "gif"; folder = "Textures"; }
    else if (begin.Contains("JFIF")) { print("Dumping asset type: JFIF"); output = "jfif"; folder = "Textures"; }
    else if (begin.Contains("OggS")) { print("Dumping asset type: OGG"); output = "ogg"; folder = "Sounds"; }
    else if (begin.Contains("TSSE") || begin.Contains("Lavf") || begin.Contains("matroska")) { print("Dumping asset type: MP3"); output = "mp3"; folder = "Sounds"; }
    else if (begin.Contains("KTX ")) { print("Dumping asset type: Khronos Texture"); output = "ktx"; folder = "KTX Textures"; }
    else if (begin.StartsWith("#EXTM3U")) { debug("Parsing EXTM3U file..."); output = "ext"; folder = "Videos"; }
    else if (begin.Contains("\"name\": \"")) { print("Dumping asset type: JSON font list"); output = "ttf"; folder = "Fonts"; }
    else if (begin.Contains("{\"applicationSettings")) { debug("Ignoring FFlag JSON file."); return; }
    else if (begin.Contains("{\"version")) { debug("Ignoring client version JSON file."); return; }
    else if (begin.Contains("webmB")) { debug("Ignoring VideoFrame segment, the dumping process is handled through another file!"); return; }
    else
    {
        warn("File unrecognized: " + begin);
        //output = "unkn";
        //folder = "Unknown";
        return;
    }
    if (!Directory.Exists(curpath + "/temp"))
    {
        system("mkdir temp >nul 2>&1");
    }
    if (!Directory.Exists("assets/" + folder))
    {
        system("mkdir \"assets/" + folder + "\" >nul 2>&1");
    }
    if (output == "ktx")
    {
        File.WriteAllBytes(curpath + "temp\\" + outhash + ".ktx", cont);
        while (true)
        {
            if (File.Exists(curpath + "temp\\" + outhash + ".ktx"))
            {
                break;
            } else
            {
                Thread.Sleep(10);
            }
        }
        system("pvrtextoolcli.exe -i \"%cd%\\temp\\" + outhash + ".ktx\" -noout -shh -d \"%cd%\\assets\\" + folder + "\\" + outhash + ".png\"");
        srgb2lin.convert("assets/" + folder + "/" + outhash + ".png");
        system("del \"%cd%\\temp\\" + outhash + ".ktx\"");
    }
    else if (output == "ttf")
    {
        JObject js = JObject.Parse(Encoding.UTF8.GetString(cont));
        var outname = js["name"];
        File.WriteAllBytes(curpath + "assets/" + folder + "/" + outname + ".json", cont);
        JArray faces = (JArray)js["faces"];
        print("Found " + faces.Count + " fonts");
        foreach(JObject item in faces)
        {
            string asset = item["assetId"].ToString();
            if (asset.Contains("rbxassetid://"))
            {
                print("Downloading " + outname + "-" + item["name"] + ".ttf...");
                var assetid = asset.Split("rbxassetid://")[1];
                byte[] fontdata = null;
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
                        warn("Font download failed, retrying...");
                    }
                }
                File.WriteAllBytes(curpath + "assets/" + folder + "/" + outname + "-" + item["name"] + ".ttf", fontdata);
            } else
            {
                if (asset.Contains("rbxasset://"))
                {
                    debug("Skipping " + outname + "-" + item["name"] + ".ttf because it is a local asset.");
                } else
                {
                    debug("Skipping " + outname + "-" + item["name"] + ".ttf because the 'rbxassetid' identifer was not found.");
                }
            }
        }
    }
    else if (output == "translation")
    {
        JObject js = JObject.Parse(Encoding.UTF8.GetString(cont));
        string locale = (string)js["locale"];
        File.WriteAllBytes(curpath + "assets/" + folder + "/locale-" + locale + ".json", cont);
    }
    else if (output == "mesh")
    {
        string meshVersion = Encoding.UTF8.GetString(cont)[..12];
        string numOnlyVer = meshVersion[8..];
        string noDotVer = numOnlyVer.Replace(".", "");
        if (BloxMesh.supported_mesh_versions.Contains(meshVersion))
        {
            debug("Converting mesh version " + numOnlyVer);
            BloxMesh.Convert(cont, folder, outhash);
        }
        else
        {
            warn("Mesh version " + numOnlyVer + " unsupported! Dumping raw file.");
            folder = "Unsupported " + folder;
            if (!Directory.Exists(curpath + "assets/" + folder))
            {
                system("mkdir \"assets/" + folder + "\" >nul 2>&1");
            }
            File.WriteAllBytes(curpath + "assets/" + folder + "/" + outhash + ".bm", cont);
        }
    }
    else if (output == "ext")
    {
        string content = Encoding.UTF8.GetString(cont);
        if (!content.Contains("RBX-BASE-URI"))
        {
            debug("Ignoring undesired EXTM3U file.");
            return;
        }
        print("Dumping asset type: VideoFrame");
        (string,string,string) M3Data = ParseEXTM3U(content);
        if(M3Data.Item1 == "")
        {
            error("No stream URL found for VideoFrame.");
            return;
        }
        print("Downloading metadata for best resolution: " + M3Data.Item2);
        string metadata = "";
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
                warn("Metadata download failed, retrying...");
            }
        }
        string hash = M3Data.Item3.Split("/").Last();
        string file = M3Data.Item1.Split("/").Last();
        string segmentUrl = M3Data.Item1.Replace(file, "");
        List<string> segments = new List<string>();
        bool nextIsASegment = false;
        foreach (string i in metadata.Split("\n"))
        {
            if(nextIsASegment)
            {
                segments.Add(i);
                nextIsASegment = false;
            } else
            {
                if (i.StartsWith("#EXTINF:"))
                {
                    nextIsASegment = true;
                }
            }
        }
        string tempdir = curpath + "temp/VideoFrame-" + hash;
        if (!Directory.Exists(tempdir))
        {
            system("mkdir \"temp/VideoFrame-" + hash + "\" >nul 2>&1");
        }
        List<string> names = new List<string>();
        bool succeeded = true;
        foreach(string i in segments)
        {
            print("Downloading " + i + " for VideoFrame (" + hash + ")");
            while (true)
            {
                var req = new RestRequest(segmentUrl + i, Method.Get);
                RestResponse resp = client.Get(req);
                if (resp.IsSuccessStatusCode)
                {
                    File.WriteAllBytes(tempdir + "/" + i, resp.RawBytes);
                    print("Repairing downloaded video...");
                    string name2 = i.Replace(".webm", "-repaired.webm");
                    names.Add("file '" + name2 + "'");
                    system($"ffmpeg.exe -i \"%cd%\\temp\\VideoFrame-{hash}\\" + i + "\" -c copy -bsf:v setts=ts=PTS-STARTPTS \"%cd%\\temp\\VideoFrame-"+hash+"\\" + name2 + "\" >nul 2>&1");
                    if (!File.Exists(tempdir + "/" + name2))
                    {
                        error("Repair failed, no output found.");
                        succeeded = false;
                        break;
                    }
                    File.Delete(tempdir + "/" + i);
                    break;
                }
                else
                {
                    warn("Video download failed, retrying...");
                }
            }
            if (!succeeded)
                break;
        }
        if (succeeded)
        {
            File.WriteAllLines(tempdir + "/" + "videos.txt", names.ToArray());
            print("Merging VideoFrame segments...");
            system($"ffmpeg -f concat -safe 0 -i \"%cd%\\temp\\VideoFrame-{hash}\\videos.txt\" -c copy \"%cd%\\assets\\" + folder + "\\" + hash + ".webm\" -y >nul 2>&1");
        }
        Directory.Delete(tempdir, true);
    }
    else if (output != null)
    {
        File.WriteAllBytes(curpath + "assets/" + folder + "/" + outhash + "." + output, cont);
    }
}

bool threading = true;

if (manualThreadCount)
{
    Console.Clear();
    Console.WriteLine("How many threads do you want to use?");
    Console.WriteLine("Your CPU has " + Environment.ProcessorCount + " threads. Please input a number less or equal to it.");
    Console.WriteLine("More threads = faster dumping & more CPU usage.");
    while (true)
    {
        Console.Write("\nInput: ");
        string input = Console.ReadLine();
        int desiredThreads;
        if (int.TryParse(input, out desiredThreads))
        {
            if (desiredThreads > Environment.ProcessorCount)
            {
                Console.WriteLine("\nAre you sure you want to use this amount of threads?");
                Console.Write("\nType Y to confirm: ");
                if (Console.ReadLine().ToLower() == "y")
                {
                    Console.WriteLine();
                    max_threads = desiredThreads;
                    break;
                }
            }
            else
            {
                Console.WriteLine();
                max_threads = desiredThreads;
                break;
            }
        }
        else
        {
            Console.WriteLine("Invalid input!");
        }
    }
}

Console.Clear();

if (promptCacheClear)
{
    Console.WriteLine("Do you want to clear Roblox's cache?");
    Console.WriteLine("If you clear the cache then any assets downloaded from previous sessions will not be dumped.");
    Console.Write("\nType Y to clear or anything else to proceed: ");
    if (Console.ReadLine().ToLower() == "y")
    {
        Console.WriteLine();
        print("Deleting Roblox cache...");
        system("del " + usePath + "* /q");
        Console.Clear();
    }
} else
{
    if(autoClear)
    {
        print("Deleting Roblox cache...");
        system("del " + usePath + "* /q");
        Console.Clear();
    }
}

print("BloxDump started.");

Console.Title = client_name+" | Idle";

while (true)
{
    int counts = 0;
    if(panic)
    {
        Console.Title = client_name + " | Waiting";
        bool iDir1 = Directory.Exists(webPath);
        bool iDir2 = Directory.Exists(UWPPath);
        if(iDir1)
        {
            usePath = webPath; panic = false; continue;
        } else if(iDir2)
        {
            usePath = UWPPath; panic = false; continue;
        } else
        {
            warn("Waiting for temp directory to show up (P)...");
            Thread.Sleep(5000);
        }
    }
    if (Directory.Exists(usePath))
    {
        string[] files = Directory.GetFiles(usePath);
        int total = files.Length;
        foreach (string i in files)
        {
            string name = i.Split("\\http\\")[1];
            counts++;
            Console.Title = client_name + " | Processing file " + counts + "/" + total + " (" + name + ")";
            if (!name.StartsWith("RBX"))
            {
                if (!known.Contains(name))
                {
                    known.Add(name);
                    if (threading)
                    {
                        check_thread_life();
                        while (threads.Count >= max_threads)
                        {
                            Thread.Sleep(50);
                            check_thread_life();
                        }
                        Thread thr = new Thread(() => thread(i));
                        thr.Start();
                        lock (lockObject)
                        {
                            threads.Add(thr);
                        }
                    }
                    else
                    {
                        thread(i);
                    }
                }
            }
            else
            {
                warn("Ignoring temporary file: " + name);
            }
        }
    } else
    {
        Console.Title = client_name + " | Waiting";
        warn("Waiting for temp directory to show up...");
    }
    Console.Title = client_name+" | Idle";
    print("Ripping loop completed.");
    Thread.Sleep(5000);
}
