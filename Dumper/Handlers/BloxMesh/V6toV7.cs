#pragma warning disable CS8602,CS8604

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Evergine.Bindings.Draco;

using static Essentials;
using static BloxMesh;

public static class V6toV7
{
    public unsafe static void Process(int whoami, string dumpName, byte[] content)
    {
        Encoding utf = Encoding.UTF8;
        string? version = utf.GetString(content[..12]);
        //print($"Dumping mesh {version} - {dumpName}");
        Stream stream = new MemoryStream(content);
        BinaryReader reader = new BinaryReader(stream);
        reader.ReadBytes(13);
        while (reader.BaseStream.Position < stream.Length)
        {
            byte[] chunkType = reader.ReadBytes(8);
            uint chunkVersion = reader.ReadUInt32();
            uint chunkSize = reader.ReadUInt32();
            uint dataSize;
            if (chunkVersion == 2)
            {
                dataSize = reader.ReadUInt32();
            } else
            {
                dataSize = chunkSize;
            }
            string chunkString = utf.GetString(chunkType);
            //print($"Chunk '{chunkString}', size: {chunkSize}");
            if(chunkString != "COREMESH")
            {
                reader.BaseStream.Position += chunkSize;
                continue;
            }
            byte[] chunkData = reader.ReadBytes((int)dataSize);
            FileMeshVertex[] vertices = new FileMeshVertex[0];
            FileMeshFace[] faces = new FileMeshFace[0];
            List<FileMeshFace> validFaces = new List<FileMeshFace>();
            if (chunkVersion == 2)
            {
                //warn("Chunk Data presumably is Draco compressed.");
                Draco.Mesh mesh;
                fixed (byte* dataPtr = chunkData)
                {
                    nuint valuePtr = (nuint)(&dataSize);
                    mesh = Draco.Decompress((nint)dataPtr, valuePtr);
                }
                vertices = new FileMeshVertex[mesh.numVertices];
                //faces = new FileMeshFace[mesh.numFaces];
                //print(mesh.numVertices);
                //print(mesh.numFaces);

                // general data
                for (int attribI = 0; attribI < mesh.numAttributes; attribI++)
                {
                    Draco.Attribute attrib = mesh.GetAttribute(attribI);
                    Draco.Data meshData = Draco.GetData(mesh, attrib);
                    //print(attrib.attributeType+" - "+meshData.dataType);
                    if (meshData.dataType == Draco.DataType.DT_FLOAT32)
                    {
                        var data = (float*)meshData.data;
                        if (attrib.attributeType == Draco.AttributeType.POSITION)
                        {
                            uint numComponents = attrib.numComponents;
                            for (int i = 0; i < mesh.numVertices; i++)
                            {
                                vertices[i].px = numComponents >= 1 ? data[i * numComponents + 0] : 0;
                                vertices[i].py = numComponents >= 2 ? data[i * numComponents + 1] : 0;
                                vertices[i].pz = numComponents >= 3 ? data[i * numComponents + 2] : 0;
                            }
                            //print("Filled vertex positions.");
                        }
                        else if (attrib.attributeType == Draco.AttributeType.TEX_COORD && attrib.numComponents == 2)
                        {
                            for (int i = 0; i < mesh.numVertices; i++)
                            {
                                vertices[i].tu = data[i * 2 + 0];
                                vertices[i].tv = data[i * 2 + 1];
                            }
                            //print("Filled UV coordinates.");
                        }
                        else if (attrib.attributeType == Draco.AttributeType.GENERIC)
                        {
                            uint numComponents = attrib.numComponents;
                            for (int i = 0; i < mesh.numVertices; i++)
                            {
                                vertices[i].nx = numComponents >= 1 ? data[i * numComponents + 0] : 0;
                                vertices[i].ny = numComponents >= 2 ? data[i * numComponents + 1] : 0;
                                vertices[i].nz = numComponents >= 3 ? data[i * numComponents + 2] : 0;
                            }
                            //print("Filled normal vectors.");
                        }
                    }
                    else if (meshData.dataType == Draco.DataType.DT_UINT8)
                    {
                        var data = (byte*)meshData.data;
                        if (attrib.attributeType == Draco.AttributeType.GENERIC && attrib.numComponents == 4)
                        {
                            for (int i = 0; i < mesh.numVertices; i++)
                            {
                                vertices[i].tx = (sbyte)data[i * 4 + 0];
                                vertices[i].ty = (sbyte)data[i * 4 + 1];
                                vertices[i].tz = (sbyte)data[i * 4 + 2];
                                vertices[i].ts = (sbyte)data[i * 4 + 3];
                            }
                            //print("Filled vertex tangents.");
                        } else if (attrib.attributeType == Draco.AttributeType.COLOR && attrib.numComponents == 4)
                        {
                            for (int i = 0; i < mesh.numVertices; i++)
                            {
                                vertices[i].r = data[i * 4 + 0];
                                vertices[i].g = data[i * 4 + 1];
                                vertices[i].b = data[i * 4 + 2];
                                vertices[i].a = data[i * 4 + 3];
                            }
                            //print("Filled color tints.");
                        }
                    }
                    Draco.Release(meshData);
                }

                // faces data
                if(mesh.numFaces > 0)
                {
                    Draco.Data faceData = mesh.GetIndices();
                    if(faceData.dataType != Draco.DataType.DT_INT32)
                    {
                        error($"Thread-{whoami}: Unexpected indices codec for Roblox Mesh! ({faceData})");
                        return;
                    }
                    uint numIndices = faceData.dataSize / sizeof(int);
                    int* indices = (int*)faceData.data;
                    for (int i = 0; i < mesh.numFaces; i++)
                    {
                        FileMeshFace tmp = new FileMeshFace();
                        tmp.a = (uint)indices[i * 3 + 0] + 1;
                        tmp.b = (uint)indices[i * 3 + 1] + 1;
                        tmp.c = (uint)indices[i * 3 + 2] + 1;
                        validFaces.Add(tmp);
                        if(tmp.a == mesh.numVertices || tmp.b == mesh.numVertices || tmp.c == mesh.numVertices)
                        {
                            // had to do this because Draco is dumb apparently >:(
                            break;
                        }
                    }
                    faces = validFaces.ToArray();
                    //print("Filled indices.");
                    Draco.Release(faceData);
                }
                Draco.Release(mesh);
            } else if(chunkVersion == 1)
            {
                if(version.Contains("7"))
                {
                    debug($"Thread-{whoami}: Version 7 meshes should not have chunk version 1, this is likely invalid. Ignoring.");
                    return;
                }
                MemoryStream ms = new MemoryStream(chunkData);
                BinaryReader br = new BinaryReader(ms);
                uint numVerts = br.ReadUInt32();
                vertices = new FileMeshVertex[numVerts];
                vertices = ReadVertices(br, vertices, numVerts, 40);
                uint numFaces = br.ReadUInt32();
                faces = new FileMeshFace[numFaces];
                for (int i = 0; i < numFaces; i++)
                {
                    faces[i].a = reader.ReadUInt32() + 1;
                    faces[i].b = reader.ReadUInt32() + 1;
                    faces[i].c = reader.ReadUInt32() + 1;
                }
                br.Dispose();
                ms.Dispose();
            } else 
            {
                warn($"Thread-{whoami}: Unsupported Roblox Mesh COREMESH version! ({chunkVersion})");
                return;
            }

            string filePath = $"assets/Meshes/{dumpName}-v{version[8..]}.obj";
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.Write("# Converted from Roblox Mesh " + version + " to obj by BloxDump\n");
                StringBuilder vertData = new StringBuilder();
                StringBuilder texData = new StringBuilder();
                StringBuilder normData = new StringBuilder();
                StringBuilder faceData = new StringBuilder();
                foreach (FileMeshVertex vert in vertices)
                {
                    appendFix(ref vertData, $"v {vert.px} {vert.py} {vert.pz}");
                    //appendFix(ref vertData, $"vc {vert.r / 255.0f} {vert.g / 255.0f} {vert.b / 255.0f}");
                    appendFix(ref normData, $"vn {vert.nx} {vert.ny} {vert.nz}");
                    appendFix(ref texData, $"vt {vert.tu} {vert.tv} 0");
                }
                foreach (FileMeshFace face in faces)
                {
                    appendFix(ref faceData, $"f {face.a}/{face.a}/{face.a} {face.b}/{face.b}/{face.b} {face.c}/{face.c}/{face.c}");
                }
                writer.Write(vertData);
                writer.Write(normData);
                writer.Write(texData);
                writer.Write(faceData);
            }

            validFaces.Clear();
            reader.Dispose();
            stream.Dispose();

            return;
        }
    }
}