using System.Diagnostics;
using System.Text;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;

#pragma warning disable CS0219
#pragma warning disable CS8321
#pragma warning disable CS8600
#pragma warning disable CS8602
#pragma warning disable CS8604

bool db = false;

void debug(string input) { if (db) { Console.WriteLine("\x1b[6;30;44m" + "DEBUG" + "\x1b[0m " + input); } }
void print(string input) { Console.WriteLine("\x1b[6;30;47m" + "INFO" + "\x1b[0m " + input); }
void warn(string input) { Console.WriteLine("\x1b[6;30;43m" + "WARN" + "\x1b[0m " + input); }
void error(string input) { Console.WriteLine("\x1b[6;30;41m" + "ERROR" + "\x1b[0m " + input); }

string curpath = System.IO.Path.GetDirectoryName(System.AppContext.BaseDirectory) + "\\";
int max_threads = Environment.ProcessorCount;
List<Task> threads = new List<Task>();

void check_thread_life()
{
    List<Task> still_alive = new List<Task>();
    foreach (Task a in threads)
    {
        if (!a.IsCompleted)
        {
            still_alive.Add(a);
        }
    }
    threads = still_alive;
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
    "isCircular"
};

using HttpClient client = new HttpClient(new HttpClientHandler
{
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
});
client.DefaultRequestHeaders.Accept.Clear();
client.DefaultRequestHeaders.Accept.Add(
    new MediaTypeWithQualityHeaderValue("binary/octet-stream"));
client.DefaultRequestHeaders.Add("User-Agent", "BloxDump");

async Task thread(string name)
{
    byte[] data = File.ReadAllBytes(name);
    string dataString = Encoding.UTF8.GetString(data[0..(data.Length > 128 ? 128 : data.Length)]);
    if (dataString.Substring(0, 4) != "RBXH")
    {
        debug("Ignoring non-RBXH file: " + name);
        return;
    }
    string link = dataString.Substring(dataString.IndexOf("https://")).Split("\x00")[0];
    if (knownlinks.Contains(link))
    {
        debug("Ignoring duplicate cdn link.");
        return;
    }
    knownlinks.Add(link);
    string[] s = link.Split("/");
    string outhash = s[s.Length - 1];
    if (bans.Contains(outhash))
    {
        debug("Ignoring blocked hash.");
        return;
    }
    byte[] cont = null;
    while (true)
    {
        HttpResponseMessage resp = await client.GetAsync(link);
        if (resp.IsSuccessStatusCode)
        {
            cont = await resp.Content.ReadAsByteArrayAsync();
            break;
        }
        else
        {
            warn("Download failed, retrying...");
        }
    }
    string begin = Encoding.UTF8.GetString(cont[..48]);
    string output = null;
    string folder = null;
    if (begin.Contains("<roblox!"))
    {
        print("Data identified as RBXM Animation");
        output = "rbxm";
        folder = "Animations";
    }
    else if (begin.Contains("<roblox xml"))
    {
        debug("Ignoring unsupported XML file.");
        return;
    }
    else if (begin.Contains("version"))
    {
        print("Data identified as a Roblox Mesh");
        output = "mesh";
        folder = "Meshes";
    }
    else if (begin.Contains("{\"locale\":\""))
    {
        print("Data identified as JSON translation");
        output = "translation";
        folder = "Translations";
    }
    else if (begin.Contains("PNG\r\n"))
    {
        print("Data identified as PNG");
        output = "png";
        folder = "Textures";
    }
    else if (begin.Contains("JFIF"))
    {
        print("Data identified as JFIF");
        output = "jfif";
        folder = "Textures";
    }
    else if (begin.Contains("OggS"))
    {
        print("Data identified as OGG");
        output = "ogg";
        folder = "Sounds";
    }
    else if (begin.Contains("matroska"))
    {
        print("Data identified as Matroska? Assuming MP3 output");
        output = "mp3";
        folder = "Sounds";
    }
    else if (begin.Contains("KTX "))
    {
        print("Data identified as Khronos Texture");
        output = "ktx";
        folder = "KTX Textures";
    }
    else if (begin.Contains("\"name\": \""))
    {
        print("Data identified as JSON font list");
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
        //return;
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
        var js = JsonObject.Parse(System.Text.Encoding.UTF8.GetString(cont));
        var outname = js["name"];
        File.WriteAllBytes(curpath + "assets/" + folder + "/" + outname + ".json", cont);
        Thread.Sleep(100);
        print("Found " + js["faces"].ToString().Length + " fonts");
        for (int j = 0; j < js["faces"].ToString().Length; j++)
        {
            print("Downloading " + outname + "-" + js["faces"][j]["name"] + ".ttf...");
            var assetid = js["faces"][j]["assetId"].ToString().Split("rbxassetid://")[1];
            byte[] fontdata = null;
            while (true)
            {
                HttpResponseMessage resp = await client.GetAsync("https://assetdelivery.roblox.com/v1/asset?id=" + assetid);
                if (resp.IsSuccessStatusCode)
                {
                    fontdata = await resp.Content.ReadAsByteArrayAsync();
                    break;
                }
                else
                {
                    warn("Download failed, retrying...");
                }
            }
            File.WriteAllBytes(curpath + "assets/" + folder + "/" + outname + "-" + js["faces"][j]["name"] + ".ttf", fontdata);
        }
    }
    else if (output == "translation")
    {
        var js = JsonObject.Parse(System.Text.Encoding.UTF8.GetString(cont));
        var locale = js["locale"];
        File.WriteAllBytes(curpath + "assets/" + folder + "/locale-" + locale + ".json", cont);
    }
    else if (output == "mesh")
    {
        string meshVersion = System.Text.Encoding.UTF8.GetString(cont)[..12];
        string numOnlyVer = meshVersion[8..];
        string noDotVer = numOnlyVer.Replace(".", "");
        if (BloxMesh.supported_mesh_versions.Contains(meshVersion))
        {
            print("Converting mesh version " + numOnlyVer);
            BloxMesh.Convert(cont, folder, outhash);
        }
        else
        {
            print("Mesh version " + numOnlyVer + " unsupported! Dumping raw file.");
            folder = "Unsupported " + folder;
            if (!Directory.Exists(curpath + "assets/" + folder))
            {
                system("cd \"" + curpath + "\" && mkdir \"assets/" + folder + "\" >nul 2>&1");
            }
            File.WriteAllBytes(curpath + "assets/" + folder + "/" + outhash + ".bm" + noDotVer, cont);
        }
    }
    else if (output != null)
    {
        File.WriteAllBytes(curpath + "assets/" + folder + "/" + outhash + "." + output, cont);
    }
}

Console.Clear();
system("cls");
//those clears ensure that the print labels work
Console.Title = "BloxDump | Prompt";
print("Thread limit: "+max_threads+" threads.");
print("Please note that this is intended to utilize maximum CPU to dump assets and adjusts to your CPU.\n");
Console.WriteLine("Do you want to clear Roblox's cache?");
Console.WriteLine("Clearing cache will prevent ripping of anything from previous game sessions.");
Console.WriteLine("Do this if you want to let BloxRip work in real-time while you're playing.");
Console.Write("Type Y to clear or anything else to proceed: ");
if (Console.ReadLine().ToLower() == "y")
{
    Console.WriteLine();
    print("Deleting Roblox cache...");
    system("del " + tempPath + "* /q");
}
Console.Clear();
Console.Title = "BloxDump | Idle";
print("BloxDump started.");

while (true)
{
    int counts = 0;
    string[] files = Directory.GetFiles(tempPath);
    int total = files.Length;
    foreach (string i in files)
    {
        string name = i.Split("\\http\\")[1];
        counts++;
        Console.Title = "BloxDump | Processing file " + counts + "/" + total + " (" + name + ")";
        if (!name.StartsWith("RBX"))
        {
            if (!known.Contains(name))
            {
                known.Add(name);
                check_thread_life();
                while (threads.Count >= max_threads)
                {
                    Thread.Sleep(100);
                    check_thread_life();
                }
                threads.Add(Task.Run(() => thread(i)));
            }
        }
        else
        {
            warn("Ignoring temporary file: " + name);
        }
    }
    Console.Title = "BloxDump | Idle";
    print("Ripping loop completed.");
    Thread.Sleep(10000);
}