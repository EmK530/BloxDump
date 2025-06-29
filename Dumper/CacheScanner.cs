using System.ComponentModel;
using System.Data.SQLite;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;
using static Essentials;

class CacheScanner
{
    public static string targetPath = webPath;
    public static bool TargetIsDatabase = webIsDatabase;
    public static string dbFolder = webDB;

    public static async Task<bool> Begin()
    {
        while (true)
        {
            await PerformScan();
            await Task.Delay(5000);
        }
    }

    private static List<string> known = new List<string>();
    private static HashSet<string> ignoreSet = new HashSet<string>(known);

    public static async Task PerformScan()
    {
        bool hasWarned = false;
        bool file_exists = File.Exists(targetPath);
        if (TargetIsDatabase ? file_exists : Directory.Exists(targetPath))
        {
            int found = 0;
            bool changed = false;

            if (!TargetIsDatabase)
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
            } else
            {
                try
                {
                    using (var connection = new SQLiteConnection("Data Source=" + targetPath))
                    {
                        connection.Open();
                        string query = "SELECT id, content FROM files";
                        var command = new SQLiteCommand(query, connection);
                        var reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            if (reader["id"] is byte[] id)
                            {
                                string hash = BitConverter.ToString(id).Replace("-", "").ToLowerInvariant();
                                if (!ignoreSet.Contains(hash))
                                {
                                    string byte1 = hash.Substring(0, 2);
                                    if (reader["content"] is byte[] test)
                                    {
                                        // found content, send directly to dumper
                                        changed = true;
                                        known.Add(hash);
                                        found += 1;
                                        await Dumper.EnqueueAsset(hash, test);
                                    }
                                    else
                                    {
                                        // no content, fetch from folder
                                        string finalPath = $"{dbFolder}{byte1}\\{hash}";
                                        if (File.Exists(finalPath))
                                        {
                                            changed = true;
                                            known.Add(hash);
                                            found += 1;
                                            await Dumper.EnqueueAsset(finalPath);
                                        }
                                        else
                                        {
                                            debug($"Could not find hash {hash} in rbx-storage.");
                                            changed = true;
                                            known.Add(hash);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!hasWarned)
                    {
                        hasWarned = true;
                        if (ex.ToString().Contains("0x87AF03F3"))
                        {
                            warn($"No access to read target database! Falling back to temp method.\n{targetPath}\n{ex}");
                        }
                        else
                        {
                            warn($"Target database may be corrupt! Falling back to temp method.\n{targetPath}\n{ex}");
                        }
                        TargetIsDatabase = false;
                        targetPath = $"{tempDir}Roblox\\http\\";
                    }
                }
            }

            if (changed)
                ignoreSet = new HashSet<string>(known);
            if (found > 0)
                print($"Queued {found} cache{((found == 1) ? "" : "s")}.");
            else
                print("Found no new caches.");
        }
        else
        {
            print("Cache target not found, try opening Roblox?");
        }
    }
}