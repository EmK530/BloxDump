#pragma warning disable CS8600
#pragma warning disable CS8602
#pragma warning disable CS8604

/*

BloxMesh co-developed by zuzaratrust
many thanks :)
 
*/

using System.Text;
using static Essentials;

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
        "version 4.01",
        "version 5.00",
        "version 6.00",
        "version 7.00"
    };

    public struct FileMeshVertex
    {
        public float px, py, pz;     // Position
        public float nx, ny, nz;     // Normal Vector
        public float tu, tv, tw;     // UV Texture Coordinates

        public sbyte tx, ty, tz, ts; // Tangent Vector & Bi-Normal Direction
        public byte r, g, b, a;      // RGBA Color Tinting
    }

    public struct FileMeshFace
    {
        public uint a; // 1st Vertex Index
        public uint b; // 2nd Vertex Index
        public uint c; // 3rd Vertex Index
    }

    public static FileMeshVertex[] ReadVertices(BinaryReader reader, FileMeshVertex[] verts, uint count, byte szvertex)
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
            verts[i].tv = 1.0f - reader.ReadSingle();
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

    public static void appendFix(ref StringBuilder b, string insert)
    {
        b.AppendLine(insert.Replace(",", "."));
    }

    public static async Task Convert(int whoami, string dumpName, byte[] content)
    {
        string meshVersion = Encoding.UTF8.GetString(content)[..12];
        string numOnlyVer = meshVersion[8..];
        if (!supported_mesh_versions.Contains(meshVersion))
        {
            warn($"Thread-{whoami}: Mesh version unsupported: {numOnlyVer}");
            if (!Directory.Exists("assets/Unsupported Meshes"))
            {
                Directory.CreateDirectory("assets/Unsupported Meshes");
            }
            await File.WriteAllBytesAsync($"assets/Unsupported Meshes/{dumpName}-v{numOnlyVer}.mesh",content);
            return;
        }
        if(!Directory.Exists("assets/Meshes"))
        {
            Directory.CreateDirectory("assets/Meshes");
        }
        if (File.Exists($"assets/Meshes/{dumpName}-v{numOnlyVer}.obj"))
        {
            debug($"Thread-{whoami}: Skipping already dumped Roblox Mesh.");
            return;
        }
        print($"Thread-{whoami}: Converting Roblox Mesh version {numOnlyVer}...");
        try
        {
            switch (numOnlyVer)
            {
                case "1.00":
                case "1.01":
                    await v1.Process(whoami, dumpName, content);
                    break;
                case "2.00":
                    await v2.Process(whoami, dumpName, content);
                    break;
                case "3.00":
                case "3.01":
                    await v3.Process(whoami, dumpName, content);
                    break;
                case "4.00":
                case "4.01":
                    await v4.Process(whoami, dumpName, content);
                    break;
                case "5.00":
                    await v5.Process(whoami, dumpName, content);
                    break;
                case "6.00":
                case "7.00":
                    V6toV7.Process(whoami, dumpName, content); // unsafe due to Draco, cannot be async
                    break;
            }
        } catch(Exception ex)
        {
            error($"Thread-{whoami}: Failed to convert Roblox Mesh! ({dumpName})");
            error($"Thread-{whoami}: {ex.Message}");
            error(ex.InnerException);
        }
    }
}