using System.Diagnostics;
using System.Text;

#pragma warning disable CS0219
#pragma warning disable CS8321
#pragma warning disable CS8600
#pragma warning disable CS8602

bool db = true;

void debug(string input) { Console.WriteLine("\x1b[6;30;44m" + "DEBUG" + "\x1b[0m " + input); }
void print(string input) { Console.WriteLine("\x1b[6;30;47m" + "INFO" + "\x1b[0m " + input); }
void warn(string input) { Console.WriteLine("\x1b[6;30;43m" + "WARN" + "\x1b[0m " + input); }
void error(string input) { Console.WriteLine("\x1b[6;30;41m" + "ERROR" + "\x1b[0m " + input); }

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
    print(link);
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