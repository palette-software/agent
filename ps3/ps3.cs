using System;
using System.IO;
using System.Text;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

//
// This application uses the following Environment Variables:
//   ACCESS_KEY, SECRET_KEY, SESSION, REQUEST_ENDPOINT
//
class ps3
{
    static int usage()
    {
        Console.Error.WriteLine("usage: ps3 [GET|PUT] <bucket> <file-or-key>");
        return -1;
    }

 
    static int doGET(AmazonS3Client client, string bucketName, string key)
    {
        GetObjectRequest request = new GetObjectRequest
        {
            BucketName = bucketName,
            Key = key
        };

        string dest = key;
        using (GetObjectResponse response = client.GetObject(request))
        {
                response.WriteResponseStreamToFile(dest);
        }
        return 0;
    }

    static int doPUT(AmazonS3Client client, string bucketName, string path)
    {
        if (!File.Exists(path))
        {
            // This exception is also thrown if path specifies a non-regular file (e.g. a directory)
            throw new FileNotFoundException(path);
        }

        string key = Path.GetFileName(path);

        PutObjectRequest request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = key,
            FilePath = path
        };

        PutObjectResponse response = client.PutObject(request);
        // The response does not require any cleanup.

        return 0;
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

        string sessionToken = Environment.GetEnvironmentVariable("SESSION");

        RegionEndpoint regionEndpoint = RegionEndpoint.USEast1;
        string systemName = Environment.GetEnvironmentVariable("REGION_ENDPOINT");
        if (systemName != null)
        {
            regionEndpoint = RegionEndpoint.GetBySystemName(systemName);
        }

        AmazonS3Client client;
        if (sessionToken == null)
        {
            client = new AmazonS3Client(accessKey, secretKey, regionEndpoint);
        }
        else
        {
            client = new AmazonS3Client(accessKey, secretKey, sessionToken, regionEndpoint);
        }

        string method = args[0].ToUpper();
        try
        {
            switch (method)
            {
                case "GET":
                    return doGET(client, args[1], args[2]);
                case "PUT":
                    return doPUT(client, args[1], args[2]);
                default:
                    return usage();
            }
        }
        catch (AmazonS3Exception e)
        {
            Console.Error.WriteLine(e.ToString());
            return -1;
        }
        catch (FileNotFoundException e)
        {
            Console.Error.WriteLine(e.ToString());
            return -1;
        }
    }
}
