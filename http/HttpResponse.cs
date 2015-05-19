using System;
using System.IO;
using System.Collections;
using System.Text;

/// <summary>
/// Encapsulates an HTTP response object
/// </summary>
public class HttpResponse
{
    public const string VERSION = "1.1";
    public const string SERVER = "Palette/0.0";

    protected HttpStreamWriter writer;

    public MemoryStream body = new MemoryStream();
    public int StatusCode = 200;
    public string StatusDescription = "OK";
    public Hashtable Headers = new Hashtable();
    public string ContentType = "text/plain";

    public bool needClose = false;
    public bool needRestart = false;

    //This has to be put in each class for logging purposes
    private static readonly log4net.ILog logger = log4net.LogManager.GetLogger
    (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="writer">a StreamWriter</param>
    public HttpResponse(HttpStreamWriter writer)
    {
        this.writer = writer;
    }

    /// <summary>
    /// Writes response body
    /// </summary>
    /// <param name="bytes"></param>
    public void Write(byte[] bytes)
    {
        body.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="start"></param>
    /// <param name="count"></param>
    public void Write(byte[] bytes, int start, int count)
    {
        body.Write(bytes, start, count);
    }

    /// <summary>
    /// Writes response body
    /// </summary>
    /// <param name="s"></param>
    public void Write(string s)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(s);
        Write(bytes);
    }

    /// <summary>
    /// Flushes output of StreamWriter
    /// </summary>
    public void Flush()
    {
        writer.WriteLine("HTTP/" + VERSION + " " + Convert.ToString(StatusCode) + " " + StatusDescription);
        writer.WriteLine("Server: " + SERVER);
        writer.WriteLine("Content-Type: " + ContentType);
        writer.WriteLine("Content-Length: " + Convert.ToString(body.Length));
        writer.WriteLine("");
        if (body.Length > 0)
        {
            byte[] bytes = body.GetBuffer();
            writer.Write(bytes, 0, (int)body.Length);
        }

        writer.Flush();
    }
}