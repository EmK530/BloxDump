using static Essentials;

class CacheScanner
{
    public static string targetPath = webPath;
    public static bool TargetIsSharded = webIsSharded;

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
        if(Directory.Exists(targetPath))
        {
            int found = 0;
            bool changed = false;

            if (TargetIsSharded)
            {
                foreach (string dir in Directory.GetDirectories(targetPath))
                {
                    if (dir.StartsWith("p")) // these folders did not seem to have anything RBXH related
                        continue;
                    foreach (string i in Directory.GetFiles(dir))
                    {
                        string name = Path.GetFileName(i);
                        if (!ignoreSet.Contains(name))
                        {
                            changed = true;
                            known.Add(name);
                            found += 1;
                            await Dumper.EnqueueAsset(i);
                        }
                    }
                }
            }
            else
            {
                foreach (string i in Directory.GetFiles(targetPath))
                {
                    string name = Path.GetFileName(i);
                    if (!ignoreSet.Contains(name))
                    {
                        changed = true;
                        known.Add(name);
                        found += 1;
                        await Dumper.EnqueueAsset(i);
                    }
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
