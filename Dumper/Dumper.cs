using System.ComponentModel.Design;
using System.Threading.Channels;
using static Essentials;

public static class Dumper
{
    public static CancellationToken token;

    public struct Asset
    {
        public ulong Id;
        public string Location;
        public string Type;
    }
    public struct Cache
    {
        public string Path;
        public byte[] Data;
    }

    public static Channel<Cache> queue = Channel.CreateUnbounded<Cache>();

    private static void UpdateTitle()
    {
        Console.Title = $"{app_name} {app_version} | Queued assets: {queue.Reader.Count}";
    }

    public static async Task EnqueueAsset(string path)
    {
        await queue.Writer.WriteAsync(new Cache()
        {
            Path = path
        });
        UpdateTitle();
    }

    public static async Task EnqueueAsset(string path, byte[] data)
    {
        await queue.Writer.WriteAsync(new Cache()
        {
            Path = path,
            Data = data
        });
        UpdateTitle();
    }

    public static async Task Thread(int whoami)
    {
        /*
        using var httpClient = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
            AllowAutoRedirect = true
        });
        */

        while (!token.IsCancellationRequested)
        {
            // try-catch ReadAsync in case queue is killed and an error is thrown
            Cache asset;
            try
            {
                UpdateTitle();
                asset = await queue.Reader.ReadAsync();
            } catch
            {
                continue;
            }

            byte[] content;
            string dumpName = "";
            string link = "";

            string path = asset.Path;
            ParsedCache data = ParseCache(asset);
            if (!data.success)
                continue;
            content = data.content;
            link = data.link;

            if (UseCDNHashFilenames) {
                string[] s = link.Split(".com/");
                dumpName = s[s.Length - 1];
                if (dumpName.Contains("?"))
                {
                    dumpName = dumpName.Split("?")[0];
                }
                if (dumpName.Contains("DAY-"))
                {
                    dumpName = dumpName.Split("DAY-")[1];
                }
                string[] s2 = dumpName.Split("/");
                dumpName = s2[0];
            } else {
                dumpName = Path.GetFileNameWithoutExtension(path);
            }
            //print($"Thread-{whoami}: Dumping asset hash {dumpName}...");

            var res = IdentifyContent(content);
            switch(res.Item1)
            {
                case AssetType.Unknown:
                    warn($"Thread-{whoami}: Unrecognized header: {res.Item2} | {dumpName}");
                    break;
                case AssetType.Ignored:
                    debug($"Thread-{whoami}: {res.Item3}.");
                    break;
                case AssetType.NoConvert:
                    {
                        string outDir = $"assets/{res.Item4}";
                        if (!Directory.Exists(outDir))
                        {
                            Directory.CreateDirectory(outDir);
                        } else if(File.Exists($"{outDir}/{dumpName}.{res.Item2}"))
                        {
                            debug($"Thread-{whoami}: Skipping already dumped {res.Item3}.");
                            continue;
                        }
                        print($"Thread-{whoami}: Saving asset type: {res.Item3}");
                        await File.WriteAllBytesAsync($"{outDir}/{dumpName}.{res.Item2}",content);
                    }
                    break;
                case AssetType.WebP:
                    {
                        string outDir = $"assets/{res.Item4}";
                        if(link.Contains("-AvatarHeadshot-") || link.Contains("-Avatar-"))
                        {
                            debug($"Thread-{whoami}: Skipping avatar image.");
                            continue;
                        } else
                        {
                            if (!Directory.Exists(outDir))
                            {
                                Directory.CreateDirectory(outDir);
                            } else if (File.Exists($"{outDir}/{dumpName}.{res.Item2}")) {
                                debug($"Thread-{whoami}: Skipping already dumped WebP.");
                                continue;
                            }
                            print($"Thread-{whoami}: Saving asset type: {res.Item3}");
                        }
                        await File.WriteAllBytesAsync($"{outDir}/{dumpName}.{res.Item2}", content);
                    }
                    break;
                case AssetType.Mesh:
                    {
                        await BloxMesh.Convert(whoami, dumpName, content);
                    }
                    break;
                case AssetType.FontList:
                    {
                        await FontList.Process(whoami, dumpName, content);
                    }
                    break;
                case AssetType.Translation:
                    {
                        await Translation.Process(whoami, dumpName, content);
                    }
                    break;
                case AssetType.EXTM3U:
                    {
                        await EXTM3U.Process(whoami, dumpName, content);
                    }
                    break;
                case AssetType.Khronos:
                    {
                        await Khronos.Process(whoami,dumpName,content);
                    }
                    break;
            }
        }
    }
}