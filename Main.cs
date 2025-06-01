#pragma warning disable CS8602,CS8604

using Newtonsoft.Json.Linq;
using static Essentials;

class Entry
{
    public static async Task Main(string[] args)
    {
        //Assets.Init();
        Console.Title = $"{app_name} {app_version}";

        CancellationTokenSource cts = new CancellationTokenSource(); // currently unused lol
        Dumper.token = cts.Token;

        List<Task> threads = new List<Task>();

        if (!webPath.EndsWith("\\"))
            webPath = webPath + "\\";
        if (!UWPPath.EndsWith("\\"))
            UWPPath = UWPPath + "\\";

        // Manage dependencies
        if (!Directory.Exists(dependDir))
        {
            Directory.CreateDirectory(dependDir);
        }
        if(!File.Exists(dependDir + "ffmpeg.exe"))
        {
            bool downloaded = await DownloadDependencyAsync("ffmpeg.exe", "https://drive.usercontent.google.com/download?id=1uflkRywFWnySw3oohWoQmwDzhcqpbq0U&export=download&confirm=t");
            if (!downloaded)
                fatal("Could not download dependency: ffmpeg.exe");
        }
        if(Directory.GetCurrentDirectory().Contains("System32"))
        {
            warn("Attempting to move out of System32!");
            Directory.SetCurrentDirectory(exedir);
        }

        if (ReadConfigBoolean("Cache.ForceCustomDirectory.Enable"))
        {
            string dir = ReadAliasedString("Cache.ForceCustomDirectory.TargetDirectory");
            if (!Directory.Exists(dir))
            {
                fatal($"The config enabled dumping from a custom directory, but it did not exist! ({dir})\nFix the directory or disable ForceCustomDirectory.");
            }
            if (!dir.EndsWith("\\"))
                dir += "\\";
            CacheScanner.targetPath = dir;
            CacheScanner.TargetIsSharded = ReadConfigBoolean("Cache.ForceCustomDirectory.IsSharded");
        }
        else
        {
            bool isDir1 = Directory.Exists(webPath);
            bool isDir2 = Directory.Exists(UWPPath);

            if (isDir1 && isDir2)
            {
                Console.WriteLine("\nWhich version of Roblox do you use?");
                Console.WriteLine("If your input is incorrect then dumping will not work as intended.\n");
                Console.WriteLine($"1: Standard Version\n2: UWP Version (Microsoft Store)");
                bool done = false;
                while (!done)
                {
                    Console.Write("\nInput: ");
                    string? get = Console.ReadLine();
                    if (int.TryParse(get, out int parsed))
                    {
                        switch (parsed)
                        {
                            case 1:
                                CacheScanner.targetPath = webPath;
                                CacheScanner.TargetIsSharded = webIsSharded;
                                done = true;
                                break;
                            case 2:
                                CacheScanner.targetPath = UWPPath;
                                CacheScanner.TargetIsSharded = UWPisSharded;
                                done = true;
                                break;
                            default:
                                warn($"Unrecognized choice: {parsed}");
                                break;
                        }
                    }
                }
            }
            else if (isDir2 && !isDir1)
            {
                CacheScanner.targetPath = UWPPath;
            }
            else if (!isDir1 && !isDir2)
            {
                fatal($"Found no existing cache directory for Roblox, make sure you've ran the game before launching BloxDump.");
            }
        }

        if (ReadConfigBoolean("Cache.PromptClearOnLaunch"))
        {
            Console.WriteLine("\nDo you want to clear Roblox's cache?");
            Console.WriteLine("If you clear the cache then any assets downloaded from previous sessions will not be dumped.");
            Console.Write("\nType Y to clear or anything else to proceed: ");
            if (Console.ReadLine().ToLower() == "y")
            {
                Console.WriteLine();
                print("Deleting Roblox cache...");
                EmptyFolder(CacheScanner.targetPath);
            }
        }
        else
        {
            if (ReadConfigBoolean("Cache.AutoClearIfNoPrompt"))
            {
                print("Deleting Roblox cache...");
                EmptyFolder(CacheScanner.targetPath);
            }
        }

        int threadCount = Environment.ProcessorCount - 1;
        if (ReadConfigBoolean("DumperSettings.CustomThreadCount.Enable"))
        {
            threadCount = ReadConfigInteger("DumperSettings.CustomThreadCount.Target");
            if (threadCount > Environment.ProcessorCount)
            {
                warn($"The config has instructed BloxDump to run with {threadCount} threads.");
                warn($"This is more than your CPU thread count of {Environment.ProcessorCount}, be aware.");
            }
        }

        for (int i = 0; i < Math.Max(threadCount, 1); i++)
        {
            int thr = i + 1;
            debug($"Launching dumper Thread-{thr}");
            threads.Add(Task.Run(() => Dumper.Thread(thr)));
        }

        Console.Clear();

        await CacheScanner.Begin();
    }
}