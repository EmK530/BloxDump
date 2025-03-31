using static Essentials;

class CacheScanner
{
    public static string targetPath = webPath;

    public static bool panic = false;

    public static async Task<bool> Begin()
    {
        while(true)
        {
            await PerformScan();
            await Task.Delay(5000);
        }
    }

    private static List<string> known = new List<string>();
    private static HashSet<string> ignoreSet = new HashSet<string>(known);

    public static async Task PerformScan()
    {
        if(panic)
        {
            Console.Title = $"{app_name} {app_version} | Waiting";
            bool iDir1 = Directory.Exists(webPath);
            bool iDir2 = Directory.Exists(UWPPath);
            if (iDir1)
            {
                targetPath = webPath; panic = false;
            }
            else if (iDir2)
            {
                targetPath = UWPPath; panic = false;
            }
            else
            {
                warn("Waiting for temp directory to show up...");
                return;
            }
        }
        if(Directory.Exists(targetPath))
        {
            int found = 0;
            bool changed = false;
            foreach(string i in Directory.GetFiles(targetPath))
            {
                string name = Path.GetFileName(i);
                if(!ignoreSet.Contains(name))
                {
                    changed = true;
                    known.Add(name);
                    found += 1;
                    await Dumper.EnqueueAsset(i);
                }
            }
            if (changed)
                ignoreSet = new HashSet<string>(known);
            if(found>0)
                print($"Queued {found} asset{((found==1)?"":"s")}.");
            else
                print("Found no new assets.");
        } else
        {
            warn("Cannot scan for caches because the folder does not exist.");
        }
    }
}