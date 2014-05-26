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
                string drive = line.Substring(0, 1).ToUpper();
                routes[drive] = line;
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
