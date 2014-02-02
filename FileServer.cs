using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

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

    protected override void Handle(HttpListenerContext ctx)
    {
        HttpListenerRequest req = ctx.Request;

        string path = UriToPath(req.Url.PathAndQuery);
        if (!File.Exists(path)) {
            throw new HttpNotFound();
        }

        string mimeType = GuessMimeType(Path.GetExtension(path));
        ctx.Response.ContentType = mimeType;

        // FIXME: do this in chunks.
        byte[] data = File.ReadAllBytes(path);
        ctx.Response.OutputStream.Write(data, 0, data.Length);
    }

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
