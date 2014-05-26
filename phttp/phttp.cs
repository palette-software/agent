using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.ComponentModel;

class phttp
{
    public const int BUFFER_SIZE = 1024 * 1024;

    static string BuildFilePath(Uri uri, string path)
    {
        return Path.Combine(path, Path.GetFileName(uri.LocalPath));
    }

    static string CreateAuthorization(string realm, string userName, string password)
    {
        string auth = ((realm != null) && (realm.Length > 0) ?
        realm + @"\" : "") + userName + ":" + password;
        auth = Convert.ToBase64String(Encoding.Default.GetBytes(auth));
        return auth;
    }

    static string GetCredentials()
    {
        string userName = Environment.GetEnvironmentVariable("BASIC_USERNAME");
        if (userName == null)
        {
            return null;
        }
        string password = Environment.GetEnvironmentVariable("BASIC_PASSWORD");
        if (password == null)
        {
            return null;
        }
        string realm = Environment.GetEnvironmentVariable("BASIC_REALM");
        return CreateAuthorization(realm, userName, password);
    }

    static int GET(Uri uri, string path)
    {
        if (path == null)
        {
            path = Directory.GetCurrentDirectory();
        }

        string filePath = BuildFilePath(uri, path);
        string tmpFilePath = filePath + ".tmp";

        string auth = GetCredentials();

        //Download the file
        WebClient client = new WebClient();
        if (auth != null)
        {
            client.Headers.Add("Authorization: Basic " + auth);
        }
        client.DownloadFile(uri, tmpFilePath);

        File.Move(tmpFilePath, filePath);
        Console.WriteLine("Download of file " + filePath + " completed.");

        return 0;
    }

    static int PUT(Uri uri, string path)
    {
        if (path == null)
        {
            return usage();
        }

        FileInfo fi = new FileInfo(path);

        HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(uri);
        req.Method = "PUT";
        req.ContentLength = fi.Length;
        req.KeepAlive = true;

        string auth = GetCredentials();
        if (auth != null)
        {
            req.Headers["Authorization"] = "Basic " + auth;
        }

        Stream stream = req.GetRequestStream();
        byte[] buffer = new byte[phttp.BUFFER_SIZE];

        using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
        {
            long length = fi.Length;

            while (length > 0)
            {
                int count = reader.Read(buffer, 0, (int)Math.Max(length, buffer.Length));
                stream.Write(buffer, 0, count);
                length -= count;
            }
        }
        stream.Close();

        HttpWebResponse res = (HttpWebResponse)req.GetResponse();
        Console.WriteLine(res.StatusDescription);
        return 0;
    }

    static int usage()
    {
        Console.Error.WriteLine("Usage: phttp GET|PUT <URL> [source-or-destination]");
        return -1;
    }

    static int Main(string[] args)
    {
        //System.Diagnostics.Debugger.Launch();

        if (args.Length < 2 || args.Length > 3)
        {
            return usage();
        }

        string method = args[0].ToUpper();
        string url = args[1];

        string path = (args.Length == 3) ? args[2] : null;

        try
        {
            Uri uri = new Uri(url);

            //Accept all (especially self-signed) certificates
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            switch (method)
            {
                case "GET":
                    return GET(uri, path);
                case "PUT":
                    return PUT(uri, path);
            }

            Console.Error.WriteLine("[ERROR] Invalid METHOD");
            return usage();
        }
        catch (Exception exc)
        {
            Console.Error.WriteLine(exc.ToString());
            return -1;
        }
    }
}
