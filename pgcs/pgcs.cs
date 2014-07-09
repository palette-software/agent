using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

class pgcs
{
    static int usage()
    {
        Console.Error.WriteLine("usage: pgcs [GET|PUT] <bucket> <file-or-key>");
        return -1;
    }

    static int doGET(GCS gcs, string bucketName, string key)
    {
        string path = key;
        gcs.GET(bucketName, key, path);
        return 0;
    }

    static int doPUT(GCS gcs, string bucketName, string path)
    {
        if (!File.Exists(path))
        {
            // This exception is also thrown if path specifies a non-regular file (e.g. a directory)
            throw new FileNotFoundException(path);
        }

        int result = gcs.PUT(bucketName, path);
        return result == 200 ? 0 : -1;
    }

    static int Main(string[] args)
    {
        //System.Diagnostics.Debugger.Launch();

        if (args.Length != 3)
        {
            return usage();
        }

        string accessKey = Environment.GetEnvironmentVariable("ACCESS_KEY");
        if (accessKey == null)
        {
            Console.Error.WriteLine("[ERROR] The environment variable ACCESS_KEY is required.");
            return -1;
        }

        string secretKey = Environment.GetEnvironmentVariable("SECRET_KEY");
        if (secretKey == null)
        {
            Console.Error.WriteLine("[ERROR] The environment variable SECRET_KEY is required.");
            return -1;
        }

        GCS gcs = new GCS(accessKey, secretKey);

        string method = args[0].ToUpper();
        try
        {
            switch (method)
            {
                case "GET":
                    return doGET(gcs, args[1], args[2]);
                case "PUT":
                    return doPUT(gcs, args[1], args[2]);
                default:
                    return usage();
            }
        }
        catch (WebException e)
        {
            if (e.Response != null)
            {
                using (StreamReader reader = new StreamReader(e.Response.GetResponseStream()))
                {
                    Console.Error.WriteLine(reader.ReadToEnd());
                }
            }
            throw(e);
        }
    }
}
