using System;
using System.Collections.Generic;
using System.IO;

class CGIRouter
{
    protected string path;
    public Dictionary<string, string> routes = new Dictionary<string, string>();

    public CGIRouter(string path)
    {
        this.path = path;
        if (path != null) {
            Parse(path);
        } else {
            // Create a default route to the 'Data' subdirectory.
            AddRoute(GetDataDir());
        }
    }

    private string GetDataDir()
    {
        string path = Path.GetDirectoryName(Environment.GetEnvironmentVariable("SCRIPT_FILENAME"));
        path = StdPath.Combine(path, "..", "data");
        return Path.GetFullPath(path);
    }

    private void AddRoute(string path)
    {
        string drive = path.Substring(0, 1).ToUpper();
        routes[drive] = path;
    }

    private void Parse(string path) {
        string line;
        using (StreamReader reader = new StreamReader(path)) {
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }
                // FIXME: support UNC Paths
                AddRoute(line);
            }
        }
}

    public string Get(string key)
    {
        key = key.ToUpper();
        if (!routes.ContainsKey(key))
        {
            return null;
        }
        return routes[key];
    }
}
