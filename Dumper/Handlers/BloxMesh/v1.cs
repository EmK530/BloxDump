#pragma warning disable CS8602,CS8604

using System.Text;
using System.Text.Json.Nodes;

using static Essentials;
using static BloxMesh;

public static class v1
{
    public static async Task Process(int whoami, string dumpName, byte[] data)
    {
        using (var reader = new StringReader(Encoding.UTF8.GetString(data)))
        {
            string? version = reader.ReadLine();
            int num_faces = int.Parse(reader.ReadLine());
            var content = JsonObject.Parse("[" + reader.ReadLine().Replace("][", "],[") + "]");
            int true_faces = content.AsArray().Count / 3;
            debug($"Thread-{whoami}: Mesh is version " + version + " and has " + num_faces + " faces.");
            string filePath = $"assets/Meshes/{dumpName}-v{version[8..]}.obj";
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                await writer.WriteAsync("# Converted from Roblox Mesh " + version + " to obj by BloxDump\n");
                StringBuilder vertData = new StringBuilder();
                StringBuilder texData = new StringBuilder();
                StringBuilder normData = new StringBuilder();
                StringBuilder faceData = new StringBuilder();
                for (int i = 0; i < true_faces; i++)
                {
                    var vert = content[i * 3];
                    var norm = content[i * 3 + 1];
                    var uv = content[i * 3 + 2];
                    appendFix(ref vertData, $"v {(double)vert[0]} {(double)vert[1]} {(double)vert[2]}");
                    appendFix(ref normData, $"vn {(double)norm[0]} {(double)norm[1]} {(double)norm[2]}");
                    appendFix(ref texData, $"vt {(double)uv[0]} {1.0 - (double)uv[1]} {(double)uv[2]}");
                }
                for (int i = 0; i < (true_faces - 1) / 3; i++)
                {
                    var pos = (i * 3 + 1);
                    appendFix(ref faceData, $"f {pos}/{pos}/{pos} {pos + 1}/{pos + 1}/{pos + 1} {pos + 2}/{pos + 2}/{pos + 2}");
                }
                await writer.WriteAsync(vertData);
                await writer.WriteAsync(normData);
                await writer.WriteAsync(texData);
                await writer.WriteAsync(faceData);
            }
        }
    }
}