using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class HttpStream : IDisposable
{
    public byte CR = 0xD;
    public byte LF = 0xA;

    protected Stream BaseStream;
    protected bool CloseBaseStream = true;

    public HttpStream(Stream stream)
    {
        BaseStream = stream;
    }

    public HttpStream(Stream stream, bool closeBaseStream)
        : this(stream)
    {
        CloseBaseStream = closeBaseStream;
    }

    public void Close()
    {
        if (CloseBaseStream)
        {
            BaseStream.Close();
        }
    }

    public void Dispose()
    {
        if (CloseBaseStream)
        {
            BaseStream.Dispose();
        }
    }
}

// Wrapper class to read both text and (unbuffered) binary data.
// Not derived from Reader but acts like one.
public class HttpStreamReader : HttpStream
{
    public HttpStreamReader(Stream stream) : base(stream) {}
    public HttpStreamReader(Stream stream, bool closeBaseStream) : base(stream, closeBaseStream) { }
    
    // reads lines ending in CRLF //
    public string ReadLine()
    {
        List<byte> bytes = new List<byte>();
        int current, last = 0;
        while ((current = Read()) != -1 && last != (int)CR && current != (int)LF)
        {
            byte b = (byte)current;
            bytes.Add(b);
            last = (int)b;
        }
        if (last == CR)
        {
            bytes.RemoveAt(bytes.Count - 1);
        }
        return Encoding.ASCII.GetString(bytes.ToArray());
    }

    // Read works differently than the `Read()` method of a 
    // TextReader. It reads the next BYTE rather than the next character
    public int Read()
    {
        return BaseStream.ReadByte();
    }

    /// <summary>
    /// Reader binary data as bytes.
    /// </summary>
    /// <param name="buffer">An array of bytes.</param>
    /// <param name="index">The zero-based byte offset to use in buffer.</param>
    /// <param name="count">The total number of bytes to read into the buffer.</param>
    /// <returns></returns>
    public int Read(byte[] buffer, int index, int count)
    {
        return BaseStream.Read(buffer, index, count);
    }

    /// <summary>
    /// Read 'length' bytes and return the result as a byte array.
    /// </summary>
    /// <param name="length"></param>
    /// <returns></returns>
    public byte[] Read(int length)
    {
        int index = 0, n;
        byte [] bytes = new byte[length];
        while (length > 0)
        {
            n = Read(bytes, index, length);
            if (n == 0)
            {
                throw new IOException("Insufficient available bytes.");
            }
            index += n;
            length -= n;
        }
        return bytes;
    }

    /// <summary>
    /// Read 'length' bytes and convert the result into a string using the ASCII encoding.
    /// </summary>
    /// <param name="length"></param>
    /// <returns></returns>
    public string ReadText(int length)
    {
        byte[] bytes = Read(length);
        return Encoding.ASCII.GetString(bytes);
    }
}


// Wrapper class to write both text and (unbuffered) binary data.
// Not derived from Writer but acts like one.
public class HttpStreamWriter : HttpStream
{
    public HttpStreamWriter(Stream stream) : base(stream) { }
    public HttpStreamWriter(Stream stream, bool closeBaseStream) : base(stream, closeBaseStream) { }

    public void WriteLine()
    {
        BaseStream.WriteByte(CR);
        BaseStream.WriteByte(LF);
    }

    public void WriteLine(string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        BaseStream.Write(bytes, 0, bytes.Length);
        WriteLine();
    }

    public void Write(byte[] buffer, int offset, int count)
    {
        BaseStream.Write(buffer, offset, count);
    }

    public void Flush()
    {
        BaseStream.Flush();
    }
}