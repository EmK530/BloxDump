namespace KTX1;

public class ByteReader(byte[] data)
{
    private readonly MemoryStream stream = new(data);

    ~ByteReader()
    {
        stream.Dispose();
    }

    public void SkipBytes(int amount) => stream.Seek(amount, SeekOrigin.Current);
    public void SkipBytes(uint amount) => SkipBytes((int)amount);

    public uint ReadU32()
    {
       byte[] buf = new byte[sizeof(uint)];
       stream.Read(buf, 0, sizeof(uint));
       return BitConverter.ToUInt32(buf);
    }
    
    public byte[] ReadBytes(int length)
    {
       byte[] buf = new byte[length];
       stream.Read(buf, 0, length);
       return buf;
    }
    
    public byte[] ReadBytes(uint length) => ReadBytes((int)length);
}