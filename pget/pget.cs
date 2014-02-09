using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.ComponentModel;


class pget
{
    static void Main(string[] args)
    {
        string url = "";
        string localFolder = "";
        string fileName;
        string tmpFileName;

        if (args.Length < 1)
        {
            Console.WriteLine("Usage: PGet UrlToCopy [LocalFolderToCopyTo]");
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

            //Download the file
            client.DownloadFile(uri, tmpFileName);

            File.Move(tmpFileName, fileName);
            Console.WriteLine("Download of file " + fileName + " completed.");
        }
        catch (Exception exc)
        {
            Console.WriteLine("Error in download: " + exc.ToString());
        }
    }
}
