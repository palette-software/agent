using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// Create a class to normalize forward versus back slashes in PATH values.
/// This class behaves like the System.IO.Path class from .NET4.5.
/// </summary>

public static class StdPath
{
    public static string Combine(params string[] paths)
    {
        if (paths == null) throw new ArgumentNullException("paths");
        if (paths.Length == 0) throw new ArgumentException("Lists 'paths' may not be zero length.");

        string path = paths[0];

        for (int i = 0; i < paths.Length; i++)
        {
            path = Path.Combine(path, paths[i]);
        }
        return Normalize(path);
    }

    public static string Combine(string path1, string path2)
    {
        string path = Path.Combine(path1, path2);
        return Normalize(path);
    }

    public static string Normalize(string path)
    {
        return path.Replace('/', '\\');
    }
}