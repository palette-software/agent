using System;
using System.IO;
using System.Text;

using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

//
// This application uses the following Environment Variables:
//   ACCESS_KEY, SECRET_KEY, SESSION, REQUEST_ENDPOINT, PROXY_PROTOCOL, PROXY_HOST, PROXY_PORT
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

        string dest = Path.GetFileName(key);
        using (GetObjectResponse response = client.GetObject(request))
        {
                response.WriteResponseStreamToFile(dest);
        }
        return 0;
    }

    static int doMultipartPUT(AmazonS3Client client, string bucketName, string path, string key)
    {
        TransferUtility fileTransferUtility = new TransferUtility(client);
        fileTransferUtility.Upload(path, bucketName, key);
        return 0;
    }

    static int doPUT(AmazonS3Client client, string bucketPath, string path)
    {
        if (!File.Exists(path))
        {
            // This exception is also thrown if path specifies a non-regular file (e.g. a directory)
            throw new FileNotFoundException(path);
        }

        string[] tokens = bucketPath.Split("/".ToCharArray());
        string bucketName = tokens[0];

        string key = "";
        for (int i = 1; i < tokens.Length; i++)
        {
            key += tokens[i] + "/";
        }
        key += Path.GetFileName(path);

        FileInfo fi = new FileInfo(path);
        if (fi.Length > 100 * 1024 * 1024)
        {
            /* Use multipart file uploads if the file size exceeds 100MB as per AWS documentation. */
            return doMultipartPUT(client, bucketName, path, key);
        }

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

        AmazonS3Config config = new AmazonS3Config();
        AWSCredentials credentials;
        
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
        if (sessionToken != null) {
            credentials = new SessionAWSCredentials(accessKey, secretKey, sessionToken);
        } else {
            credentials = new BasicAWSCredentials(accessKey, secretKey);
        }

        RegionEndpoint regionEndpoint = RegionEndpoint.USEast1;
        string systemName = Environment.GetEnvironmentVariable("REGION_ENDPOINT");
        if (systemName != null)
        {
            regionEndpoint = RegionEndpoint.GetBySystemName(systemName);
        }
        config.RegionEndpoint = regionEndpoint;

        string proxyHost = Environment.GetEnvironmentVariable("PROXY_HOST");
        if (proxyHost != null)
        {
            config.ProxyHost = proxyHost;
        }

        string proxyPort = Environment.GetEnvironmentVariable("PROXY_PORT");
        if (proxyPort != null)
        {
            try
            {
                config.ProxyPort = Int16.Parse(proxyPort);
            }
            catch (FormatException ex)
            {
                Console.Error.WriteLine("[ERROR] Invalid PROXY_PORT value : " + ex.Message);
                return -1;
            }
        }

        string proxyProtocol = Environment.GetEnvironmentVariable("PROXY_PROTOCOL");
        if (proxyProtocol != null)
        {
            proxyProtocol = proxyProtocol.ToUpper();
            if (proxyProtocol == "HTTP")
            {
                config.UseHttp = true;
            }
            else if (proxyProtocol != "HTTPS")
            {
                Console.Error.WriteLine("[ERROR] PROXY_PROTOCOL must be HTTP or HTTPS");
                return -1;
            }
        }

        AmazonS3Client client = new AmazonS3Client(credentials, config);

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
