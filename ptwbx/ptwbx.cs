using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;


class ptwbx
{
    static int usage()
    {
        Console.Error.WriteLine("usage: ptwbx <file>.twbx");
        return -1;
    }

    static int Main(string[] args)
    {
        //System.Diagnostics.Debugger.Launch();

        if (args.Length != 1)
        {
            return usage();
        }

        string path = args[0];
        if (!File.Exists(path))
        {
            Console.Error.WriteLine("File Not Found: " + path);
            return -1;
        }

        string name = Path.GetFileNameWithoutExtension(path);
        string dirPath = Path.GetDirectoryName(path);
        if (dirPath == null)
        {
            dirPath = Path.GetPathRoot(path);
        }
        bool found = false;

        /* .NET 3.5 doesn't provide 'ZipFile' so use the thirdparty ZipStorer. */
        // Open an existing zip file for reading
        ZipStorer zip = ZipStorer.Open(args[0], FileAccess.Read);

        // Read the central directory collection
        List<ZipStorer.ZipFileEntry> dir = zip.ReadCentralDir();

        // Look for the desired file
        foreach (ZipStorer.ZipFileEntry entry in dir)
        {
            if (!entry.FilenameInZip.EndsWith(".twb"))
            {
                continue;
            }

            zip.ExtractFile(entry, Path.Combine(dirPath, name + ".twb"));
            found = true;
            break;
        }
        zip.Close();

        if (!found)
        {
            Console.Error.WriteLine("The zipfile does not contain a .twb file.");
            return -1;
        }

        return 0;
    }
}
