using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class CGIRequest
{
    public Dictionary<string, string> environ;

    public string uri
    {
        get
        {
            return environ["REQUEST_URI"];
        }
    }

    public string method
    {
        get
        {
            return environ["REQUEST_METHOD"].ToUpper();
        }
    }

    public CGIRequest()
    {
        environ = new Dictionary<string, string>();
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            environ[entry.Key.ToString()] = entry.Value.ToString();
        }
        SetTOPDIR();
    }

    private void SetTOPDIR()
    {
        // Just assume that cgi-bin is at the same directory level as 'conf'.
        string script = environ["SCRIPT_FILENAME"];
        string path = Path.GetDirectoryName(script);
        path = StdPath.Combine(path, "..");
        environ["TOPDIR"] = Path.GetFullPath(path);
    }

    public string[] ParsedURI()
    {
        string uri = this.uri;
        if (uri.StartsWith("/"))
        {
            uri = uri.Substring(1);
        }
        return uri.Split('/');
    }

    public void PrintEnv()
    {
        foreach (KeyValuePair<string, string> entry in environ.OrderBy(key => key.Key))
        {
            Console.WriteLine("{0}={1}", entry.Key, entry.Value);
        }
    }
}
