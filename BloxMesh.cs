using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

#pragma warning disable CS8600
#pragma warning disable CS8602
#pragma warning disable CS8604


public static class BloxMesh
{
    public static string[] supported_mesh_versions =
    {
        "version 1.00",
        "version 1.01",
        // "version 2.00"
    };
    private static string curpath = System.IO.Path.GetDirectoryName(System.AppContext.BaseDirectory) + "\\";

    private static void system(string cmd)
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

    private static bool db = false;

    private static void debug(string input) { if (db) { Console.WriteLine("\x1b[6;30;44m" + "DEBUG" + "\x1b[0m " + input); } }
    private static void print(string input) { Console.WriteLine("\x1b[6;30;47m" + "INFO" + "\x1b[0m " + input); }
    private static void warn(string input) { Console.WriteLine("\x1b[6;30;43m" + "WARN" + "\x1b[0m " + input); }
    private static void error(string input) { Console.WriteLine("\x1b[6;30;41m" + "ERROR" + "\x1b[0m " + input); }

    private static void version1(byte[] data, string folderName, string outhash)
    {
        using (var reader = new StringReader(Encoding.UTF8.GetString(data)))
        {
            string? version = reader.ReadLine();
            int num_faces = Int32.Parse(reader.ReadLine());
            var content = JsonObject.Parse("[" + reader.ReadLine().Replace("][", "],[") + "]");
            int true_faces = content.AsArray().Count / 3;
            debug("[BloxMesh_v1] Mesh is version " + version + " and has " + num_faces + " faces.");
            if (Directory.Exists(curpath + "assets/" + folderName))
            {
                system("cd \"" + curpath + "\" && mkdir \"assets/" + folderName + "\" >nul 2>&1");
            }
            var fileOut = File.Open(curpath + "assets/" + folderName + "/" + outhash + ".obj", FileMode.OpenOrCreate);
            fileOut.Write(Encoding.UTF8.GetBytes("# Converted from Roblox Mesh " + version + " to obj by BloxDump"));
            string vertData = "";
            string texData = "";
            string normData = "";
            string faceData = "";
            int loops = 0;
            for (int i = 0; i < true_faces; i++)
            {
                loops++;
                var vert = content[i * 3];
                var norm = content[i * 3 + 1];
                var uv = content[i * 3 + 2];
                if (version == "version 1.00")
                {
                    vertData=vertData.Insert(vertData.Length, "\nv " + (double)vert[0] / 2 + " " + (double)vert[1] / 2 + " " + (double)vert[2] / 2).Replace(",", ".");
                }
                else
                {
                    vertData=vertData.Insert(vertData.Length, "\nv " + (double)vert[0] + " " + (double)vert[1] + " " + (double)vert[2]).Replace(",", ".");
                }
                normData=normData.Insert(normData.Length, "\nvn " + (double)norm[0] + " " + (double)norm[1] + " " + (double)norm[2]).Replace(",", ".");
                texData=texData.Insert(texData.Length, "\nvt " + (double)uv[0] + " " + (1.0 - (double)uv[1]) + " " + (double)uv[2]).Replace(",",".");
            }
            for (int i = 0; i < (loops - 1) / 3; i++)
            {
                var pos = (i * 3 + 1);
                faceData=faceData.Insert(faceData.Length, "\nf " + pos + "/" + pos + "/" + pos + " " + (pos + 1) + "/" + (pos + 1) + "/" + (pos + 1) + " " + (pos + 2) + "/" + (pos + 2) + "/" + (pos + 2));
            }
            fileOut.Write(Encoding.UTF8.GetBytes(vertData));
            fileOut.Write(Encoding.UTF8.GetBytes(normData));
            fileOut.Write(Encoding.UTF8.GetBytes(texData));
            fileOut.Write(Encoding.UTF8.GetBytes(faceData));
            fileOut.Close();
        }
    }

    public static void Convert(byte[] data, string folderName, string outhash)
    {
        string meshVersion = Encoding.UTF8.GetString(data)[..12];
        string numOnlyVer = meshVersion[8..];
        if (!supported_mesh_versions.Contains(meshVersion))
        {
            error("Attempt to convert unsupported mesh.");
            return;
        }
        if (numOnlyVer == "1.00" || numOnlyVer == "1.01")
        {
            version1(data, folderName, outhash);
        }
    }
}