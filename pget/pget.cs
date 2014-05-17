using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.ComponentModel;


class pget
{
    static int Main(string[] args)
    {
        string url = "";
        string localFolder = "";
        string fileName;
        string tmpFileName;

        //System.Diagnostics.Debugger.Launch();

        if (args.Length < 1)
        {
            Console.Error.WriteLine("Usage: PGet UrlToCopy [LocalFolderToCopyTo]");
            return -1;
        }
        else
        {
            url = args[0];
            if (args.Length == 2)
            {
                localFolder = args[1];
            }
            else
            {
                localFolder = Directory.GetCurrentDirectory();
            }
        }

        WebClient client = new WebClient();        

        try
        {
            Uri uri = new Uri(url);

            string filePath = uri.LocalPath;

            //Remove the forward slash from the path if it is there
            if (filePath.StartsWith("/")) filePath = filePath.TrimStart('/');

            fileName = Path.Combine(localFolder, filePath);
            tmpFileName = fileName + ".tmp";

            //Accept all (especially self-signed) certificates
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            //Download the file
            client.DownloadFile(uri, tmpFileName);

            File.Move(tmpFileName, fileName);
            Console.Error.WriteLine("Download of file " + fileName + " completed.");

            return 0;
        }
        catch (Exception exc)
        {
            TextWriter errorWriter = Console.Error;
            errorWriter.WriteLine("Download of URL failed : " + url);
            errorWriter.WriteLine(exc.ToString());

            return -1;
        }
    }
}
