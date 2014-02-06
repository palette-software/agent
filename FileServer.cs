using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

/// <summary>
/// File handling methods.  Inherits from HttpBaseServer
/// </summary>
public class FileServer : HttpBaseServer
{
    protected static IDictionary<string, string> mappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) 
    {
        {".htm", "text/html"},
        {".html", "text/html"},
        {".txt", "text/plain"},
        {".zip", "application/x-zip-compressed"},
    };

    protected string documentRoot = "C:\\Palette\\Data";

    /// <summary>
    /// Constructor.  Calls HttpBaseServer base constructor
    /// </summary>
    /// <param name="port">Port id</param>
    /// <param name="documentRoot">Root file path</param>
    public FileServer(int port, string documentRoot) : base()
    {
        if (documentRoot != null)
        {
            this.documentRoot = documentRoot;
        }

        // FIXME: add all IP addresses here too.
        string prefix = "http://localhost:" + Convert.ToString(port) + "/";
        listener.Prefixes.Add(prefix);
    }

    public FileServer(int port) : this(port, null) { }

    /// <summary>
    /// Handles an HttpListenerRequest
    /// </summary>
    /// <param name="ctx">HttpListenerContext</param>
    protected override void Handle(HttpListenerContext ctx)
    {
        HttpListenerRequest req = ctx.Request;

        if (req.HttpMethod.ToUpper() != "GET")
        {
            throw new HttpMethodNotAllowed();
        }

        string path = UriToPath(req.Url.PathAndQuery);
        if (!File.Exists(path)) {
            throw new HttpNotFound();
        }

        Console.WriteLine("GET " + req.Url.PathAndQuery);

        string mimeType = GuessMimeType(Path.GetExtension(path));
        ctx.Response.ContentType = mimeType;

        // FIXME: do this in chunks.
        byte[] data = File.ReadAllBytes(path);
        ctx.Response.OutputStream.Write(data, 0, data.Length);
    }

    /// <summary>
    /// Converts a URI string to a filepath
    /// </summary>
    /// <param name="uri">a URI</param>
    /// <returns>a filepath</returns>
    protected string UriToPath(string uri)
    {
        string s = uri.Replace('/', Path.DirectorySeparatorChar);
        if (s[0] == Path.DirectorySeparatorChar)
        {
            s = s.Substring(1);
        }
        string path = Path.Combine(documentRoot, s);
        return path;
    }

    /// <summary>
    /// Determines MIME type based on filepath extension
    /// </summary>
    /// <param name="extension">filepath extension</param>
    /// <returns>MIME type</returns>
    protected string GuessMimeType(string extension)
    {
        if (!extension.StartsWith("."))
        {
            extension = "." + extension;
        }

        string mime;

        return mappings.TryGetValue(extension, out mime) ? mime : "application/octet-stream";
    }
}
