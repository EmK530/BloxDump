#pragma warning disable CS8602,CS8604

using System.Text;

using static Essentials;
using static BloxMesh;

public static class v2
{
    public static async Task Process(int whoami, string dumpName, byte[] data)
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
        debug($"Thread-{whoami}: Mesh is version " + version + " and has " + cfaces + " faces.");
        //debug(szmeshHeader + " MHSize, " + szvertex + " VSize, " + szface + " FSize, " + cverts + " VCount, " + cfaces + " FCount");
        FileMeshVertex[] verts = new FileMeshVertex[cverts];
        FileMeshFace[] faces = new FileMeshFace[cfaces];
        verts = ReadVertices(reader, verts, cverts, szvertex);
        for (int i = 0; i < cfaces; i++)
        {
            faces[i].a = reader.ReadUInt32() + 1;
            faces[i].b = reader.ReadUInt32() + 1;
            faces[i].c = reader.ReadUInt32() + 1;
        }
        string filePath = $"assets/Meshes/{dumpName}-v{version[8..]}.obj";
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            await writer.WriteAsync("# Converted from Roblox Mesh " + version + " to obj by BloxDump\n");
            StringBuilder vertData = new StringBuilder();
            StringBuilder texData = new StringBuilder();
            StringBuilder normData = new StringBuilder();
            StringBuilder faceData = new StringBuilder();
            foreach (FileMeshVertex vert in verts)
            {
                appendFix(ref vertData, $"v {vert.px} {vert.py} {vert.pz}");
                appendFix(ref normData, $"vn {vert.nx} {vert.ny} {vert.nz}");
                appendFix(ref texData, $"vt {vert.tu} {vert.tv} 0");
            }
            foreach (FileMeshFace face in faces)
            {
                appendFix(ref faceData, $"f {face.a}/{face.a}/{face.a} {face.b}/{face.b}/{face.b} {face.c}/{face.c}/{face.c}");
            }
            await writer.WriteAsync(vertData);
            await writer.WriteAsync(normData);
            await writer.WriteAsync(texData);
            await writer.WriteAsync(faceData);
        }
    }
}