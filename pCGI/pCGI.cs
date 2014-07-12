using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class pCGI
{
    public const int BUFFER_SIZE = 1024 * 1024;

    static int Main(string[] args)
    {
        //System.Diagnostics.Debugger.Launch();

        CGIRequest req = new CGIRequest();
        try
        {
            return Handle(req);
        }
        catch (HttpException e)
        {
            string status = e.StatusCode + " " + e.Reason;
            Console.Write("Status: " + status + '\n');
            Console.Write("Content-Type: text/plain\n\n");

            Console.Write(status + '\n');
            Console.Write(e.Body + '\n');
            Console.Write('\n');
            req.PrintEnv();
        }
        return 0;
    }

    static int Handle(CGIRequest req)
    {
        string routerPath = null;
        if (req.environ.ContainsKey("ROUTES_FILENAME"))
        {
            routerPath = req.environ["ROUTES_FILENAME"];
            if (!File.Exists(routerPath))
            {
                throw new HttpInternalServerError("ROUTE_FILENAME directive invalid : " + routerPath);
            }
        }

        CGIRouter router = new CGIRouter(routerPath);

        string[] uri = req.ParsedURI();

        if (req.uri == "/" || req.uri == "/ENV")
        {
            Console.Write("Content-Type: text/plain\n\n");
            foreach (KeyValuePair<string, string> entry in router.routes.OrderBy(key => key.Key))
            {
                Console.Write(entry.Value + '\n');
            }
            Console.Write('\n');
            req.PrintEnv();
            return 0;
        }

        if (uri[0].Length != 1)
        {
            throw new HttpNotFound();
        }

        string letter = uri[0];
        string path = router.Get(letter);

        if (path == null)
        {
            throw new HttpNotFound();
        }

        if (uri.Length > 1)
        {
            List <string> L = uri.Skip(1).ToList();
            L.Insert(0, path);
            path = StdPath.Combine(L.ToArray());
        }

        if (Directory.Exists(path))
        {
            if (req.method != "GET")
            {
                throw new HttpMethodNotAllowed();
            }
            return HandleDirectory(req, path);
        }
        else
        {
            switch (req.method)
            {
                case "GET":
                    return HandleFileGET(req, path);
                case "PUT":
                    return HandleFilePUT(req, path);
            }
            throw new HttpMethodNotAllowed();
        }
    }

    static int HandleDirectory(CGIRequest req, string path)
    {
        Console.Write("Content-Type: text-plain\n\n");

        Console.Write(path + '\n');
        Console.Write(new String('-', path.Length) + '\n');
        DirectoryInfo di = new DirectoryInfo(path);

        FileInfo[] fis = di.GetFiles();
        if (fis.Length > 0)
        {
            Console.Write("{0} File(s):\n", fis.Length);
            foreach (FileInfo fi in fis)
            {
                Console.Write(fi.Name + '\n');
            }
        }
        else
        {
            Console.Write("NO FILES\n");
        }
        Console.Write("\n");

        DirectoryInfo[] dis = di.GetDirectories();
        if (dis.Length > 0)
        {
            Console.Write("{0} Directories:\n", dis.Length);
            foreach (DirectoryInfo d in dis)
            {
                Console.Write(d.Name + '\n');
            }
            Console.Write("\n");
        }

        Console.Write(new String('-', path.Length) + '\n');
        req.PrintEnv();
        return 0;
    }

    static int HandleFileGET(CGIRequest req, string path)
    {
        if (!File.Exists(path))
        {
            Console.Error.Write("NOT FOUND: " + path + "\n");
            throw new HttpNotFound();
        }

        byte[] buffer = new byte[pCGI.BUFFER_SIZE];

        using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
        {
            string mimeType = MimeType.GetMimeType(Path.GetExtension(path));
            Console.Write("Content-Type: " + mimeType + "\n\n");

            using (Stream console = Console.OpenStandardOutput())
            {
                long length = reader.BaseStream.Length;

                while (length > 0)
                {
                    int count = reader.Read(buffer, 0, (int)Math.Min(length, buffer.Length));
                    console.Write(buffer, 0, count);
                    length -= count;
                }
            }

        }
        return 0;
    }

    static int HandleFilePUT(CGIRequest req, string path)
    {
        byte[] buffer = new byte[pCGI.BUFFER_SIZE];

        if (!req.environ.ContainsKey("CONTENT_LENGTH"))
        {
            throw new HttpBadRequest("Missing CONTENT_LENGTH");
        }
        ulong length = Convert.ToUInt64(req.environ["CONTENT_LENGTH"]);

        using (Stream stream = Console.OpenStandardInput())
        {
            string mimeType = MimeType.GetMimeType(Path.GetExtension(path));
            Console.Write("Content-Type: " + mimeType + "\n\n");

            using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
            {
                while (length > 0)
                {
                    int count = stream.Read(buffer, 0, (int)Math.Min(length, (ulong)buffer.Length));
                    writer.Write(buffer, 0, count);
                    length -= (ulong)count;
                }
            }

        }
        return 0;
    }
}
