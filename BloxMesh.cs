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
        "version 2.00"
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

    private struct v200Vertex
    {
        public float px, py, pz;     // Position
        public float nx, ny, nz;     // Normal Vector
        public float tu, tv, tw;         // UV Texture Coordinates

        public sbyte tx, ty, tz, ts; // Tangent Vector & Bi-Normal Direction
        public byte r, g, b, a;     // RGBA Color Tinting
    }
    private struct v200Face
    {
        public uint a; // 1st Vertex Index
        public uint b; // 2nd Vertex Index
        public uint c; // 3rd Vertex Index
    }

    private static void version2(byte[] data, string folderName, string outhash)
    {
        string? version = Encoding.UTF8.GetString(data[..12]);
        data = data[13..];
        Stream stream = new MemoryStream(data);
        BinaryReader reader = new BinaryReader(stream);
        ushort szmeshHeader = reader.ReadUInt16();
        byte szvertex = reader.ReadByte();
        byte szface = reader.ReadByte();
        uint cverts = reader.ReadUInt32();
        uint cfaces = reader.ReadUInt32();
        debug("[BloxMesh_v1] Mesh is version " + version + " and has " + cfaces + " faces.");
        debug(szmeshHeader + " MHSize, " + szvertex + " VSize, " + szface + " FSize, " + cverts + " VCount, " + cfaces + " FCount");
        if (!Directory.Exists(curpath + "assets/" + folderName))
        {
            system("cd \"" + curpath + "\" && mkdir \"assets/" + folderName + "\" >nul 2>&1");
        }
        var fileOut = File.Open(curpath + "assets/" + folderName + "/" + outhash + "-v" + version[8..] + ".obj", FileMode.OpenOrCreate);
        fileOut.Write(Encoding.UTF8.GetBytes("# Converted from Roblox Mesh " + version + " to obj by BloxDump"));
        try
        {
            v200Vertex[] verticies = new v200Vertex[cverts];
            v200Face[] faces = new v200Face[cfaces];
            for (int i = 0; i < cverts; i++)
            {
                verticies[i].px = reader.ReadSingle();
                verticies[i].py = reader.ReadSingle();
                verticies[i].pz = reader.ReadSingle();
                verticies[i].nx = reader.ReadSingle();
                verticies[i].ny = reader.ReadSingle();
                verticies[i].nz = reader.ReadSingle();
                verticies[i].tu = reader.ReadSingle();
                verticies[i].tv = reader.ReadSingle();
                verticies[i].tw = 0x0;
                verticies[i].tx = reader.ReadSByte();
                verticies[i].ty = reader.ReadSByte();
                verticies[i].tz = reader.ReadSByte();
                verticies[i].ts = reader.ReadSByte();
                if (szvertex == 40)
                {
                    verticies[i].r = reader.ReadByte();
                    verticies[i].g = reader.ReadByte();
                    verticies[i].b = reader.ReadByte();
                    verticies[i].a = reader.ReadByte();
                }
                else
                {
                    verticies[i].r = 0xff;
                    verticies[i].g = 0xff;
                    verticies[i].b = 0xff;
                    verticies[i].a = 0xff;
                }
            }
            for (int i = 0; i < cfaces; i++)
            {
                faces[i].a = reader.ReadUInt32()+1;
                faces[i].b = reader.ReadUInt32()+1;
                faces[i].c = reader.ReadUInt32()+1;
            }

            string vertData = "";
            string texData = "";
            string normData = "";
            string faceData = "";
            foreach (v200Vertex vert in verticies)
            {
                vertData = vertData.Insert(vertData.Length, "\nv " + vert.px + " " + vert.py + " " + vert.pz).Replace(",", ".");
                normData = normData.Insert(normData.Length, "\nvn " + vert.nx + " " + vert.ny + " " + vert.nz).Replace(",", ".");
                texData = texData.Insert(texData.Length, "\nvt " + vert.tu + " " + vert.tv + " 0").Replace(",", ".");
            }
            foreach (v200Face face in faces)
            {
                faceData = faceData.Insert(faceData.Length, "\nf " + face.a + "/" + face.a + "/" + face.a + " " + face.b + "/" + face.b + "/" + face.b + " " + face.c + "/" + face.c + "/" + face.c);
            }
            fileOut.Write(Encoding.UTF8.GetBytes(vertData));
            fileOut.Write(Encoding.UTF8.GetBytes(normData));
            fileOut.Write(Encoding.UTF8.GetBytes(texData));
            fileOut.Write(Encoding.UTF8.GetBytes(faceData));
            fileOut.Close();
        }
        catch (Exception e)
        {
            error("Error! " + e.Message);
        }
    }

    private static void version1(byte[] data, string folderName, string outhash)
    {
        using (var reader = new StringReader(Encoding.UTF8.GetString(data)))
        {
            string? version = reader.ReadLine();
            int num_faces = Int32.Parse(reader.ReadLine());
            var content = JsonObject.Parse("[" + reader.ReadLine().Replace("][", "],[") + "]");
            int true_faces = content.AsArray().Count / 3;
            debug("[BloxMesh_v1] Mesh is version " + version + " and has " + num_faces + " faces.");
            if (!Directory.Exists(curpath + "assets/" + folderName))
            {
                system("cd \"" + curpath + "\" && mkdir \"assets/" + folderName + "\" >nul 2>&1");
            }
            var fileOut = File.Open(curpath + "assets/" + folderName + "/" + outhash + "-v" + version[8..] + ".obj", FileMode.OpenOrCreate);
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
                    vertData = vertData.Insert(vertData.Length, "\nv " + (double)vert[0] / 2 + " " + (double)vert[1] / 2 + " " + (double)vert[2] / 2).Replace(",", ".");
                }
                else
                {
                    vertData = vertData.Insert(vertData.Length, "\nv " + (double)vert[0] + " " + (double)vert[1] + " " + (double)vert[2]).Replace(",", ".");
                }
                normData = normData.Insert(normData.Length, "\nvn " + (double)norm[0] + " " + (double)norm[1] + " " + (double)norm[2]).Replace(",", ".");
                texData = texData.Insert(texData.Length, "\nvt " + (double)uv[0] + " " + (1.0 - (double)uv[1]) + " " + (double)uv[2]).Replace(",", ".");
            }
            for (int i = 0; i < (loops - 1) / 3; i++)
            {
                var pos = (i * 3 + 1);
                faceData = faceData.Insert(faceData.Length, "\nf " + pos + "/" + pos + "/" + pos + " " + (pos + 1) + "/" + (pos + 1) + "/" + (pos + 1) + " " + (pos + 2) + "/" + (pos + 2) + "/" + (pos + 2));
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
        else if (numOnlyVer == "2.00")
        {
            version2(data, folderName, outhash);
        }
    }
}
