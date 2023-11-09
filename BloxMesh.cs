using System.Diagnostics;
using System.Reflection.PortableExecutable;
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
        "version 2.00",
        "version 3.00",
        "version 3.01",
        "version 4.00",
        "version 4.01"
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
    struct Envelope
    {
        byte[] bones; // size 4, I couldn't initialize
        byte[] weights; // size 4, I couldn't initialize
    }

    private static v200Vertex[] readVertices(BinaryReader reader, v200Vertex[] verts, uint count, byte szvertex)
    {
        for (int i = 0; i < count; i++)
        {
            verts[i].px = reader.ReadSingle();
            verts[i].py = reader.ReadSingle();
            verts[i].pz = reader.ReadSingle();
            verts[i].nx = reader.ReadSingle();
            verts[i].ny = reader.ReadSingle();
            verts[i].nz = reader.ReadSingle();
            verts[i].tu = reader.ReadSingle();
            verts[i].tv = reader.ReadSingle();
            verts[i].tw = 0x0;
            verts[i].tx = reader.ReadSByte();
            verts[i].ty = reader.ReadSByte();
            verts[i].tz = reader.ReadSByte();
            verts[i].ts = reader.ReadSByte();
            if (szvertex == 40)
            {
                verts[i].r = reader.ReadByte();
                verts[i].g = reader.ReadByte();
                verts[i].b = reader.ReadByte();
                verts[i].a = reader.ReadByte();
            }
            else
            {
                verts[i].r = 0xff;
                verts[i].g = 0xff;
                verts[i].b = 0xff;
                verts[i].a = 0xff;
            }
        }
        return verts;
    }

    private static void version4(byte[] data, string folderName, string outhash) // hey vsauce emk here, this also uses v200 vertex and face
    {
        string? version = Encoding.UTF8.GetString(data[..12]);
        Stream stream = new MemoryStream(data);
        BinaryReader reader = new BinaryReader(stream);
        reader.ReadBytes(13);
        ushort sizeof_MeshHeader = reader.ReadUInt16();
        if (sizeof_MeshHeader != 24)
        {
            error("[BloxMesh_v4] Mesh " + version + " had an incorrect header size: " + sizeof_MeshHeader + " bytes.");
            return;
        }
        ushort lodType = reader.ReadUInt16();
        uint numVerts = reader.ReadUInt32();
        uint numFaces = reader.ReadUInt32();
        ushort numLODs = reader.ReadUInt16();
        ushort numBones = reader.ReadUInt16();
        uint sizeof_boneNamesBuffer = reader.ReadUInt32();
        ushort numSubsets = reader.ReadUInt16();
        byte numHighQualityLODs = reader.ReadByte();
        reader.ReadByte(); // skip unused byte
        v200Vertex[] verts = new v200Vertex[numVerts];
        verts = readVertices(reader, verts, numVerts, 40);
        if (numBones > 0)
        {
            //skip bone data if present
            reader.ReadBytes((int)(numVerts * 8));
        }
        v200Face[] faces = new v200Face[numFaces];
        for (int i = 0; i < numFaces; i++)
        {
            faces[i].a = reader.ReadUInt32() + 1;
            faces[i].b = reader.ReadUInt32() + 1;
            faces[i].c = reader.ReadUInt32() + 1;
        }
        uint[] lods = new uint[numLODs];
        for (int i = 0; i < numLODs; i++)
        {
            lods[i] = reader.ReadUInt32();
        }
        //beyond this point is data in the mesh that is ignored
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
        foreach (v200Vertex vert in verts)
        {
            vertData = vertData.Insert(vertData.Length, "\nv " + vert.px + " " + vert.py + " " + vert.pz).Replace(",", ".");
            normData = normData.Insert(normData.Length, "\nvn " + vert.nx + " " + vert.ny + " " + vert.nz).Replace(",", ".");
            texData = texData.Insert(texData.Length, "\nvt " + vert.tu + " " + vert.tv + " 0").Replace(",", ".");
        }
        for (int i = 0; i < (lodType==0 ? numFaces : lods[1]); i++)
        {
            var face = faces[i];
            faceData = faceData.Insert(faceData.Length, "\nf " + face.a + "/" + face.a + "/" + face.a + " " + face.b + "/" + face.b + "/" + face.b + " " + face.c + "/" + face.c + "/" + face.c);
        }
        fileOut.Write(Encoding.UTF8.GetBytes(vertData));
        fileOut.Write(Encoding.UTF8.GetBytes(normData));
        fileOut.Write(Encoding.UTF8.GetBytes(texData));
        fileOut.Write(Encoding.UTF8.GetBytes(faceData));
        fileOut.Close();
    }

    private static void version3(byte[] data, string folderName, string outhash) // this uses v200 vertex and face structs because the only difference is LOD support!
    {
        string? version = Encoding.UTF8.GetString(data[..12]);
        data = data[13..];
        Stream stream = new MemoryStream(data);
        BinaryReader reader = new BinaryReader(stream);
        ushort szmeshHeader = reader.ReadUInt16();
        byte szvertex = reader.ReadByte();
        byte szface = reader.ReadByte();
        ushort szLOD = reader.ReadUInt16();
        ushort cLODs = reader.ReadUInt16();
        uint cverts = reader.ReadUInt32();
        uint cfaces = reader.ReadUInt32();
        debug("[BloxMesh_v3] Mesh is version " + version + " and has " + cfaces + " faces.");
        debug("[BloxMesh_v3] Version 3 mesh convertion will ONLY convert the highest level of detail.");
        debug(szmeshHeader + " MHSize, " + szvertex + " VSize, " + szface + " FSize, " + szLOD + " LODSize, " + cverts + " VCount, " + cfaces + " FCount, " + cLODs + " LODCount");
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
            verticies = readVertices(reader,verticies,cverts,szvertex);
            for (int i = 0; i < cfaces; i++)
            {
                faces[i].a = reader.ReadUInt32() + 1;
                faces[i].b = reader.ReadUInt32() + 1;
                faces[i].c = reader.ReadUInt32() + 1;
            }
            uint[] meshLODs = new uint[cLODs];
            for (int i = 0; i < cLODs; i++)
            {
                meshLODs[i] = reader.ReadUInt32();
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
            for (int i = 0; i < meshLODs[1]; i++)
            {
                var face = faces[i];
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
        debug("[BloxMesh_v2] Mesh is version " + version + " and has " + cfaces + " faces.");
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
            verticies = readVertices(reader, verticies, cverts, szvertex);
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
        else if (numOnlyVer == "3.00" || numOnlyVer == "3.01")
        {
            version3(data, folderName, outhash);
        }
        else if (numOnlyVer == "4.00" || numOnlyVer == "4.01")
        {
            version4(data, folderName, outhash);
        }
    }
}
