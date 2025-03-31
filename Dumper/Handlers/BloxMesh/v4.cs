#pragma warning disable CS8602,CS8604

using System.Text;

using static Essentials;
using static BloxMesh;

public static class v4
{
    public static async Task Process(int whoami, string dumpName, byte[] data)
    {
        string? version = Encoding.UTF8.GetString(data[..12]);
        Stream stream = new MemoryStream(data);
        BinaryReader reader = new BinaryReader(stream);
        reader.ReadBytes(13);
        ushort sizeof_MeshHeader = reader.ReadUInt16();
        if (sizeof_MeshHeader != 24)
        {
            error($"Thread-{whoami}: Mesh " + version + " had an incorrect header size: " + sizeof_MeshHeader + " bytes.");
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
        FileMeshVertex[] verts = new FileMeshVertex[numVerts];
        verts = ReadVertices(reader, verts, numVerts, 40);
        if (numBones > 0)
        {
            //skip bone data if present
            reader.ReadBytes((int)(numVerts * 8));
        }
        FileMeshFace[] faces = new FileMeshFace[numFaces];
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
            for (int i = 0; i < (lodType == 0 ? numFaces : lods[1]); i++)
            {
                var face = faces[i];
                appendFix(ref faceData, $"f {face.a}/{face.a}/{face.a} {face.b}/{face.b}/{face.b} {face.c}/{face.c}/{face.c}");
            }
            await writer.WriteAsync(vertData);
            await writer.WriteAsync(normData);
            await writer.WriteAsync(texData);
            await writer.WriteAsync(faceData);
        }
    }
}