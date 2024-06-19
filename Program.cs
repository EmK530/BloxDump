using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using RestSharp;

#pragma warning disable CS8600
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8604

bool db = true;

string client_name = "BloxDump v4.4.3";

void debug(string input) { if (db) { Console.WriteLine("\x1b[6;30;44m" + "DEBUG" + "\x1b[0m " + input); } }
void print(string input) { Console.WriteLine("\x1b[6;30;47m" + "INFO" + "\x1b[0m " + input); }
void warn(string input) { Console.WriteLine("\x1b[6;30;43m" + "WARN" + "\x1b[0m " + input); }
void error(string input) { Console.WriteLine("\x1b[6;30;41m" + "ERROR" + "\x1b[0m " + input); }

string curpath = Path.GetDirectoryName(AppContext.BaseDirectory) + "\\";
int max_threads = 1;
List<Thread> threads = new List<Thread>();
object lockObject = new object();

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
    process.StartInfo = startInfo;
    process.Start();
    process.WaitForExit();
}

string tempPath = Path.GetTempPath() + "Roblox\\http\\";
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
    if (begin.Contains("<roblox!"))
    {
        print("Dumping asset type: RBXM Animation");
        output = "rbxm";
        folder = "Animations";
    }
    else if (begin.Contains("<roblox xml"))
    {
        debug("Ignoring unsupported XML file.");
        return;
    }
    else if (!begin.Contains("\"version") && begin.Contains("version"))
    {
        print("Dumping asset type: Roblox Mesh");
        output = "mesh";
        folder = "Meshes";
    }
    else if (begin.Contains("{\"locale\":\""))
    {
        print("Dumping asset type: JSON translation");
        output = "translation";
        folder = "Translations";
    }
    else if (begin.Contains("PNG\r\n"))
    {
        print("Dumping asset type: PNG");
        output = "png";
        folder = "Textures";
    }
    else if (begin.Contains("JFIF"))
    {
        print("Dumping asset type: JFIF");
        output = "jfif";
        folder = "Textures";
    }
    else if (begin.Contains("OggS"))
    {
        print("Dumping asset type: OGG");
        output = "ogg";
        folder = "Sounds";
    }
    else if (begin.Contains("TSSE") || begin.Contains("Lavf") || begin.Contains("matroska"))
    {
        print("Dumping asset type: MP3");
        output = "mp3";
        folder = "Sounds";
    }
    else if (begin.Contains("KTX "))
    {
        print("Dumping asset type: Khronos Texture");
        output = "ktx";
        folder = "KTX Textures";
    }
    else if (begin.Contains("\"name\": \""))
    {
        print("Dumping asset type: JSON font list");
        output = "ttf";
        folder = "Fonts";
    }
    else if (begin.Contains("{\"applicationSettings"))
    {
        debug("Ignoring FFlag JSON file.");
        return;
    }
    else
    {
        warn("File unrecognized: " + begin);
        output = "unkn";
        folder = "Unknown";
        return;
    }
    if (!Directory.Exists(curpath + "/temp"))
    {
        system("cd \"" + curpath + "\" && mkdir temp >nul 2>&1");
    }
    if (!Directory.Exists("assets/" + folder))
    {
        system("cd \"" + curpath + "\" && mkdir \"assets/" + folder + "\" >nul 2>&1");
    }
    if (output == "ktx")
    {
        File.WriteAllBytes(curpath + "temp/" + outhash + ".ktx", cont);
        while (true)
        {
            if (File.Exists(curpath + "temp/" + outhash + ".ktx"))
            {
                break;
            }
        }
        system("cd \"" + curpath + "\" && pvrtextoolcli -i temp/" + outhash + ".ktx -noout -shh -d \"assets/" + folder + "/" + outhash + ".png\"");
        srgb2lin.convert("assets/" + folder + "/" + outhash + ".png");
        system("del temp\\" + outhash + ".ktx");
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
                system("cd \"" + curpath + "\" && mkdir \"assets/" + folder + "\" >nul 2>&1");
            }
            File.WriteAllBytes(curpath + "assets/" + folder + "/" + outhash + ".bm", cont);
        }
    }
    else if (output != null)
    {
        File.WriteAllBytes(curpath + "assets/" + folder + "/" + outhash + "." + output, cont);
    }
}

bool threading = false;

Console.Clear();
system("cls");
//those clears ensure that the print labels work
Console.Title = client_name+" | Prompt";
Console.WriteLine("Do you want to use multithreading?");
Console.WriteLine("Enabling it will make dumping faster but will use more CPU.");
Console.Write("\nType Y to enable multithreading: ");
if (Console.ReadLine().ToLower() == "y")
{
    Console.Clear();
    print("Multithreading enabled.\n");
    threading = true;
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

Console.WriteLine("Do you want to clear Roblox's cache?");
Console.WriteLine("If you clear the cache then any assets downloaded from previous sessions will not be dumped.");
Console.Write("\nType Y to clear or anything else to proceed: ");
if (Console.ReadLine().ToLower() == "y")
{
    Console.WriteLine();
    print("Deleting Roblox cache...");
    system("del " + tempPath + "* /q");
}
Console.Clear();
print("BloxDump started.");

Console.Title = client_name+" | Idle";

while (true)
{
    int counts = 0;
    if (Directory.Exists(tempPath))
    {
        string[] files = Directory.GetFiles(tempPath);
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
        warn("No temp path exists yet, cannot scan for files to dump.");
    }
    Console.Title = client_name+" | Idle";
    print("Ripping loop completed.");
    Thread.Sleep(10000);
}
