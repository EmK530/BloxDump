#pragma warning disable CS8604

using Newtonsoft.Json.Linq;
using static Essentials;

class Entry
{
    public static async Task Main(string[] args)
    {
        //Assets.Init();
        srgb2lin.preCompute();
        Console.Title = $"{app_name} {app_version}";

        CancellationTokenSource cts = new CancellationTokenSource(); // currently unused lol
        Dumper.token = cts.Token;

        List<Task> threads = new List<Task>();

        // fix prints
        Console.Clear();
        await system("cls");

        // Manage dependencies
        if (!Directory.Exists(tempDir+"BloxDump"))
        {
            Directory.CreateDirectory(tempDir + "BloxDump");
        }
        if(!File.Exists(dependDir + "ffmpeg.exe"))
        {
            bool downloaded = await DownloadDependencyAsync("ffmpeg.exe", "https://drive.usercontent.google.com/download?id=1uflkRywFWnySw3oohWoQmwDzhcqpbq0U&export=download&confirm=t");
            if (!downloaded)
                fatal("Could not download dependency: ffmpeg.exe");
        }
        if (!File.Exists(dependDir + "PVRTexToolCLI.exe"))
        {
            bool downloaded = await DownloadDependencyAsync("PVRTexToolCLI.exe", "https://drive.usercontent.google.com/download?id=1UnWvdqRUHxSFxHIYKDvh4sG2sj9ui8tF&export=download&confirm=t");
            if (!downloaded)
                fatal("Could not download dependency: PVRTexToolCLI.exe");
        }
        Console.Clear();

        if (File.Exists("config.json"))
        {
            JObject config = JObject.Parse(File.ReadAllText("config.json"));
            if (config.ContainsKey("BlockAvatarImages")) { BlockAvatarImages = (bool)config["BlockAvatarImages"]; }
            if (config.ContainsKey("PromptToClearCache")) { PromptCacheClear = (bool)config["PromptToClearCache"]; }
            if (config.ContainsKey("AutoClearCacheIfPromptIsDisabled")) { AutoClear = (bool)config["AutoClearCacheIfPromptIsDisabled"]; }
            if (config.ContainsKey("EnableDebugPrints")) { debugMode = (bool)config["EnableDebugPrints"]; }
        }
        else
        {
            File.WriteAllText("config.json", """
                {
                    "EnableDebugPrints": false,
                    "BlockAvatarImages": true,
                    "PromptToClearCache": true,
                    "AutoClearCacheIfPromptIsDisabled": false
                }
                """);
        }

        bool isDir1 = Directory.Exists(webPath);
        bool isDir2 = Directory.Exists(UWPPath);

        if (isDir1 && isDir2)
        {
            Dictionary<string, string> paths = new Dictionary<string, string>()
            {
                ["Standard Version (From Website)"] = webPath,
                ["UWP Version (Microsoft Store)"] = UWPPath
            };

            Console.WriteLine("Which version of Roblox do you use?");
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
                            done = true;
                            break;
                        case 2:
                            CacheScanner.targetPath = UWPPath;
                            done = true;
                            break;
                        default:
                            warn($"Unrecognized choice: {parsed}");
                            break;
                    }
                }
            }
            Console.Clear();
        }
        else if (isDir2 && !isDir1)
        {
            CacheScanner.targetPath = UWPPath;
        }
        else if (!isDir1 && !isDir2)
        {
            // no temp path exists, panic
            CacheScanner.panic = true;
        }

        if(!CacheScanner.panic)
        {
            if (PromptCacheClear)
            {
                Console.WriteLine("Do you want to clear Roblox's cache?");
                Console.WriteLine("If you clear the cache then any assets downloaded from previous sessions will not be dumped.");
                Console.Write("\nType Y to clear or anything else to proceed: ");
                if (Console.ReadLine().ToLower() == "y")
                {
                    Console.WriteLine();
                    print("Deleting Roblox cache...");
                    foreach (var file in Directory.GetFiles(CacheScanner.targetPath))
                    {
                        File.Delete(file);
                    }
                    Console.Clear();
                }
            }
            else
            {
                if (AutoClear)
                {
                    print("Deleting Roblox cache...");
                    foreach (var file in Directory.GetFiles(CacheScanner.targetPath))
                    {
                        File.Delete(file);
                    }
                    Console.Clear();
                }
            }
        }

        //for (int i = 0; i < 2; i++)
        for (int i = 0; i < Math.Max(Environment.ProcessorCount-1,1); i++)
        {
            int thr = i + 1;
            debug($"Launching dumper Thread-{thr}");
            threads.Add(Task.Run(() => Dumper.Thread(thr)));
        }

        await CacheScanner.Begin();
    }
}