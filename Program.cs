using System.Diagnostics;
using System.Text;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;

#pragma warning disable CS0219
#pragma warning disable CS8321
#pragma warning disable CS8600
#pragma warning disable CS8602

bool db = true;

void debug(string input) { Console.WriteLine("\x1b[6;30;44m" + "DEBUG" + "\x1b[0m " + input); }
void print(string input) { Console.WriteLine("\x1b[6;30;47m" + "INFO" + "\x1b[0m " + input); }
void warn(string input) { Console.WriteLine("\x1b[6;30;43m" + "WARN" + "\x1b[0m " + input); }
void error(string input) { Console.WriteLine("\x1b[6;30;41m" + "ERROR" + "\x1b[0m " + input); }

string curpath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\";

void system(string cmd)
{
    Process process = new Process();
    ProcessStartInfo startInfo = new ProcessStartInfo();
    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
    startInfo.FileName = "cmd.exe";
    startInfo.Arguments = "/C "+cmd;
    process.StartInfo = startInfo;
    process.Start();
    process.WaitForExit();
}

string tempPath = Path.GetTempPath() + "Roblox\\http\\";
List<string> known = new List<string>();
List<string> knownlinks = new List<string>();
List<string> bans = new List<string>
{
    "noFilter",
    "Png",
    "isCircular"
};

using HttpClient client = new();
client.DefaultRequestHeaders.Accept.Clear();
client.DefaultRequestHeaders.Accept.Add(
    new MediaTypeWithQualityHeaderValue("text/plain"));
client.DefaultRequestHeaders.Add("User-Agent", "BloxDump");

void thread(string name)
{
    byte[] data = File.ReadAllBytes(name);
    string dataString = Encoding.UTF8.GetString(data[0..(data.Length > 128 ? 128 : data.Length)]);
    if (dataString.Substring(0,4) != "RBXH")
    {
        debug("Ignoring non-RBXH file: "+name);
        return;
    }
    string link = dataString.Substring(dataString.IndexOf("https://")).Split("\x00")[0];
    if (knownlinks.Contains(link))
    {
        debug("Ignoring duplicate cdn link.");
        return;
    }
    else
    {
        knownlinks.Add(link);
        string[] s = link.Split("/");
        string outhash = s[s.Length - 1];
        if (bans.Contains(outhash))
        {
            debug("Ignoring blocked hash.");
            return;
        }
        else
        {
            var dl = client.GetStringAsync(link);
            while (true)
            {
                if (dl.Status == TaskStatus.Faulted)
                {
                    warn("Download failed, retrying...");
                }
                else
                {
                    break;
                }
            }
            dl.Wait();
            string cont = dl.Result;
            string begin = cont[..48];
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
                File.WriteAllText(curpath + "temp/" + outhash + ".ktx", cont);
                while (true)
                {
                    if (File.Exists(curpath + "temp/" + outhash + ".ktx"))
                    {
                        break;
                    }
                }
                system("cd \"" + curpath + "\" && pvrtextoolcliy -i temp/" + outhash + ".ktx -noout -shh -d \"assets/" + folder + "/" + outhash + ".png\"");
                srgb2lin.convert("assets/" + folder + "/" + outhash + ".png");
                system("del temp\\" + outhash + ".txt");
            } else if (output == "ttf")
            {
                var js = JsonObject.Parse(cont);
                var outname = js["name"];
                File.WriteAllText(curpath + "assets/" + folder + "/" + outname + ".json", cont);
                Thread.Sleep(100);
                print("Found " + js["faces"].ToString().Length + " fonts");
                for (int j = 0; j < js["faces"].ToString().Length; j++)
                {
                    print("Downloading " + outname + "-" + js["faces"][j]["name"]+".ttf...");
                    var assetid = js["faces"][j]["assetId"].ToString().Split("rbxassetid://")[1];
                    var dl2 = client.GetStringAsync("https://assetdelivery.roblox.com/v1/asset?id=" + assetid);
                    dl2.Wait();
                    if (dl.Status == TaskStatus.Faulted)
                    {
                        warn("Download failed.");
                        return;
                    }
                    File.WriteAllText(curpath + "assets/" + folder + "/" + outname + "-" + js["faces"][j]["name"] + ".ttf", dl2.Result);
                }
            } else if (output == "translation")
            {
                // TODO - add the rest of the support
            }
        }
    }
}

Console.Title = "BloxDump | Prompt";
warn("Multithreading is not developed yet.\n");
Console.WriteLine("Do you want to clear Roblox's cache?");
Console.WriteLine("Clearing cache will prevent ripping of anything from previous game sessions.");
Console.WriteLine("Do this if you want to let BloxRip work in real-time while you're playing.");
Console.Write("Type Y to clear or anything else to proceed: ");
if (Console.ReadLine().ToLower() == "y")
{
    print("\nDeleting Roblox cache...");
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
    foreach(string i in files)
    {
        string name = i.Split("\\http\\")[1];
        counts++;
        Console.Title = "BloxDump | Processing file "+counts+"/"+total+" ("+name+")";
        if (!name.StartsWith("RBX"))
        {
            if (!known.Contains(name))
            {
                known.Add(name);
                thread(i);
            }
        } else
        {
            warn("Ignoring temporary file: " + name);
        }
    }
    Console.Title = "BloxDump | Idle";
    print("Ripping loop completed.");
    Thread.Sleep(10000);
}

//srgb2lin.convert();
