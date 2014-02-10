using System;
using System.IO;

public class MaintServer : FileServer
{
    /// <summary>
    /// Constructor.  Calls FileServer base constructor
    /// </summary>
    /// <param name="port">Port id</param>
    /// <param name="documentRoot">Root file path</param>
    public MaintServer(int port, string documentRoot) : base(port, documentRoot) {}

    /// <summary>
    /// Converts a URI string to a filepath
    /// </summary>
    /// <param name="uri">a URI</param>
    /// <returns>a filepath</returns>
    protected override string UriToPath(string uri)
    {
        if (uri == "/")
        {
            // FIXME: this should be configurable in the INI file.
            uri = "/index.html";
        }
        return base.UriToPath(uri);
    }
}
