#pragma warning disable CS8602,CS8604

using static Essentials;

class Entry
{
    public static async Task Main(string[] args)
    {
        //Assets.Init();
        Console.Title = $"{app_name} {app_version}";

        if (!Ktx2Sharp.Ktx.Init())
            fatal("Could not intialize Ktx2Sharp! Make sure ktx.dll exists.");

        CancellationTokenSource cts = new CancellationTokenSource(); // currently unused lol
        Dumper.token = cts.Token;

        List<Task> threads = new List<Task>();

        if (!webPath.EndsWith("\\") && !webIsDatabase)
            webPath = webPath + "\\";
        if (!UWPPath.EndsWith("\\") && !UWPisDatabase)
            UWPPath = UWPPath + "\\";
        if (!webDB.EndsWith("\\"))
            webDB = webDB + "\\";
        if (!UWPdb.EndsWith("\\"))
            UWPdb = UWPdb + "\\";

        // Manage dependencies
        if (!Directory.Exists(dependDir))
        {
            Directory.CreateDirectory(dependDir);
        }
        if(!File.Exists(dependDir + "ffmpeg.exe"))
        {
            bool downloaded = await DownloadDependencyAsync("ffmpeg.exe", "https://github.com/EmK530/BloxDump/releases/download/dependencies/ffmpeg.exe");
            if (!downloaded)
                fatal("Could not download dependency: ffmpeg.exe");
        }
        if(Directory.GetCurrentDirectory().Contains("System32"))
        {
            warn("Attempting to move out of System32!");
            Directory.SetCurrentDirectory(exedir);
            Environment.CurrentDirectory = exedir;
        }

        if (ReadConfigBoolean("Cache.ForceCustomDirectory.Enable"))
        {
            string dir = ReadAliasedString("Cache.ForceCustomDirectory.TargetDirectory");
            bool db = ReadConfigBoolean("Cache.ForceCustomDirectory.IsDatabase");
            if (!(db ? File.Exists(dir) : Directory.Exists(dir)))
            {
                fatal($"ForceCustomDirectory is enabled, but a path was not found! ({dir})\nFix the config or disable ForceCustomDirectory.");
            }
            if (!dir.EndsWith("\\") && !db)
                dir += "\\";
            CacheScanner.targetPath = dir;
            CacheScanner.TargetIsDatabase = db;
            if(db)
            {
                string dbDir = ReadAliasedString("Cache.ForceCustomDirectory.DBFolder");
                if (!Directory.Exists(dir))
                {
                    fatal($"ForceCustomDirectory is enabled, but a path was not found! ({dir})\nFix the config or disable ForceCustomDirectory.");
                }
                CacheScanner.dbFolder = dbDir;
            }
        }
        else
        {
            bool isDir1 = webIsDatabase ? File.Exists(webPath) : Directory.Exists(webPath);
            bool isDir2 = UWPisDatabase ? File.Exists(UWPPath) : Directory.Exists(UWPPath);

            if ((isDir1 && isDir2) || (!isDir1 && !isDir2))
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
                                CacheScanner.TargetIsDatabase = webIsDatabase;
                                CacheScanner.dbFolder = webDB;
                                done = true;
                                break;
                            case 2:
                                CacheScanner.targetPath = UWPPath;
                                CacheScanner.TargetIsDatabase = UWPisDatabase;
                                CacheScanner.dbFolder = UWPdb;
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
                CacheScanner.TargetIsDatabase = UWPisDatabase;
                CacheScanner.dbFolder = UWPdb;
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
                print("Deleting Roblox cache...\n");
                while (!EmptyFolder())
                {
                    warn("Unable to clear Roblox's cache! You may need to close Roblox first.\n\nDo you wish to try again?");
                    Console.Write("\nType Y to try again or anything else to ignore: ");
                    if (Console.ReadLine().ToLower() == "y")
                    {
                        Console.Clear();
                    } else
                    {
                        break;
                    }
                }
            }
        }
        else
        {
            if (ReadConfigBoolean("Cache.AutoClearIfNoPrompt"))
            {
                print("Deleting Roblox cache...\n");
                if(!EmptyFolder())
                {
                    warn("Unable to clear Roblox's cache! You may need to close Roblox first.");
                    Thread.Sleep(3000);
                }
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