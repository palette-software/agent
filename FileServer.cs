using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using log4net;
using log4net.Config;

/// <summary>
/// File handling methods.  Inherits from HttpBaseServer
/// </summary>
public class FileServer : HttpBaseServer
{
    protected static IDictionary<string, string> mappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) 
    {
        {".css", "text/css"},
        {".htm", "text/html"},
        {".html", "text/html"},
        {".ico", "image/x-icon"},
        {".png", "image/png"},
        {".txt", "text/plain"},
        {".zip", "application/x-zip-compressed"}
    };

    //TODO: get from .ini
    protected string documentRoot;

    //This has to be put in each class for logging purposes
    private static readonly log4net.ILog logger = log4net.LogManager.GetLogger
    (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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

        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily.ToString() != "InterNetwork")
            {
                continue;
            }
            prefix = "http://" + ip.ToString() + ":" + Convert.ToString(port) + "/";
            listener.Prefixes.Add(prefix);
        }
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

        logger.Info("GET " + req.Url.PathAndQuery);

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
    protected virtual string UriToPath(string uri)
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
