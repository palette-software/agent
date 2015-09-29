using System;
using System.IO;
using System.IO.Compression;

/* FIXME: this code is essentially the same as ptwbx.cs */
class ptdsx
{
    const string FILE_EXT = ".tds";

    static int usage()
    {
        Console.Error.WriteLine("usage: ptdsx <file>.tdsx [output-filename]");
        return -1;
    }

    static int Main(string[] args)
    {
        //System.Diagnostics.Debugger.Launch();

        string filePath = null;
        if (args.Length == 2)
        {
            filePath = args[1];
        } else if (args.Length != 1) {
            return usage();
        }

        string zipPath = args[0];
        if (!File.Exists(zipPath))
        {
            Console.Error.WriteLine("File Not Found: " + zipPath);
            return -1;
        }

        if (filePath == null)
        {
            filePath = Path.GetFileNameWithoutExtension(zipPath) + FILE_EXT;
        }

        /* make filePath an absolute path if necessary */
        if (!Path.IsPathRooted(filePath))
        {
            string dirPath = Path.GetDirectoryName(zipPath);
            if (dirPath == null)
            {
                dirPath = Path.GetPathRoot(zipPath);
            }
            filePath = Path.Combine(dirPath, filePath);
        }

        bool found = false;
        using (ZipArchive archive = ZipFile.OpenRead(zipPath)) 
        {
            // Look for the desired file
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (!entry.FullName.EndsWith(FILE_EXT, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                File.Delete(filePath);
                entry.ExtractToFile(filePath);
                found = true;
                break;
            }
        }

        if (!found)
        {
            Console.Error.WriteLine("The zipfile does not contain a {0} file.", FILE_EXT);
            return -1;
        }

        return 0;
    }
}
