using System.Diagnostics;
using System.Text;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using System.Management;
using System.Security.Cryptography;

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
    else if (!begin.Contains("\"version") && begin.Contains("version"))
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
        //output = "unkn";
        //folder = "Unknown";
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
            File.WriteAllBytes(curpath + "assets/" + folder + "/" + outhash + ".bm", cont);
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
Console.WriteLine("How many threads do you want to use?");
Console.WriteLine("Your CPU has " + Environment.ProcessorCount + " threads. Please input a number less or equal to it.");
Console.WriteLine("More threads = faster, more CPU usage.");
while (true)
{
    Console.Write("\nInput: ");
    string input = Console.ReadLine();
    int desiredThreads;
    if (int.TryParse(input, out desiredThreads))
    {
        if (desiredThreads > max_threads)
        {
            Console.WriteLine("\nAre you sure you want to use this amount of threads?");
            Console.Write("Type Y to confirm: ");
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
print("Thread limit: " + max_threads + " threads.\n");

#if !DEBUG

/*

THIS BLACKLIST HAS BEEN CREATED IN RESPONSE TO A FEW SMALL CONCERNED CREATORS
NO LARGE GAMES ARE ON HERE, DO NOT WORRY ABOUT CIRCUMVENTING AND PLEASE RESPECT THESE CREATORS

ROBLOX COMMAND LINE ONLY RETRIEVED TO DETECT PLACE ID

*/

static string GetCommandLine(int processId)
{
    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}"))
    using (ManagementObjectCollection objects = searcher.Get())
    {
        foreach (ManagementBaseObject obj in objects)
        {
            var property = obj.Properties["CommandLine"];
            if (property != null)
            {
                return property.Value.ToString();
            }
        }
    }
    return null;
}

static string PBKDF2Hash(string input)
{
    byte[] bytes = Encoding.UTF8.GetBytes(input);
    ulong sd;
    if (!ulong.TryParse(input, out sd))
    {
        throw new Exception("Something went wrong checking the blacklist, please report as issue ID 1.");
    }
    int seed = (int)(sd % 2147483648);
    Random random = new Random(seed);
    byte[] salt = new byte[16];
    random.NextBytes(salt);
    int iterations = 1000000;
    using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(bytes, salt, iterations, HashAlgorithmName.SHA256))
    {
        byte[] hash = pbkdf2.GetBytes(32);
        string hashBase64 = Convert.ToBase64String(hash);
        return hashBase64;
    }
}

print("Please wait...");
HttpResponseMessage dl = await client.GetAsync("https://api.emk530.net/BDGAMEBLOCK.json");
if (!dl.IsSuccessStatusCode)
{
    Console.Clear();
    if ((int)dl.StatusCode == 521)
    {
        error("Could not retrieve blacklist because the API is down. Please wait for the server to come online.");
    } else
    {
        error("Something went wrong with the blacklist download, error code: " + (int)dl.StatusCode);
    }
    Console.ReadLine();
    Environment.Exit(1);
}
JsonArray ar = JsonObject.Parse((await dl.Content.ReadAsStringAsync())).AsArray();
Process[] processes = Process.GetProcessesByName("RobloxPlayerBeta");
if(processes.Length > 0 && Directory.GetFiles(tempPath).Length > 0)
{
    Console.Clear();
    print("BloxDump dumps assets while you play, to get a fresh start please close Roblox.");
    while(processes.Length > 0)
    {
        processes = Process.GetProcessesByName("RobloxPlayerBeta");
        Thread.Sleep(250);
    }
}
print("Deleting Roblox cache...");
system("del " + tempPath + "* /q");
Console.Clear();
print("Open Roblox to begin dumping.");
while (processes.Length == 0)
{
    processes = Process.GetProcessesByName("RobloxPlayerBeta");
    Thread.Sleep(250);
}
print("Verifying permission to dump...");
string cmd = GetCommandLine(processes[0].Id);
string placeId = "";
if (!cmd.Contains("&placeId="))
{
    if(!cmd.Contains("%26placeId%3D")){
        error("Could not find a placeId parameter in the Roblox command line, did you join from the website?");
        Console.ReadLine();
        Environment.Exit(2);
    } else {
        placeId = cmd.Split("%26placeId%3D")[1].Split("%26")[0];
    }
} else {
    placeId = cmd.Split("&placeId=")[1].Split("&")[0];
}
ulong outparse;
if (placeId == "")
{
    error("Roblox command line placeId parameter was empty.");
    Console.ReadLine();
    Environment.Exit(3);
}
if (!ulong.TryParse(placeId,out outparse))
{
    error("Roblox command line placeId parameter is not a valid number.");
    Console.ReadLine();
    Environment.Exit(4);
}
string hashed = PBKDF2Hash(placeId);
foreach (string i in ar)
{
    if (i == hashed)
    {
        Console.Clear();
        warn("Per the request of this game's creator, you cannot dump assets from this place.");
        Console.ReadLine();
        Environment.Exit(2);
    }
}
print("Permission granted, BloxDump ready.");

#else

Console.WriteLine("Do you want to clear Roblox's cache?");
Console.WriteLine("Clearing cache will prevent ripping of anything from previous game sessions.");
Console.WriteLine("Do this if you want to let BloxDump work in real-time while you're playing.");
Console.Write("Type Y to clear or anything else to proceed: ");
if (Console.ReadLine().ToLower() == "y")
{
    Console.WriteLine();
    print("Deleting Roblox cache...");
    system("del " + tempPath + "* /q");
}
Console.Clear();
print("BloxDump started.");

#endif

Console.Title = "BloxDump | Idle";

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
