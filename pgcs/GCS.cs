using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Security.Cryptography;

class GCS
{
    public const string DEFAULT_TARGET = "commondatastorage.googleapis.com";

    private string accessKey;
    private string secretKey;
    private string target;

    public GCS(string accessKey, string secretKey, string target)
    {
        this.accessKey = accessKey;
        this.secretKey = secretKey;
        if (target == null || target == String.Empty)
        {
            this.target = DEFAULT_TARGET;
        }
    }

    public GCS(string accessKey, string secretKey) : this(accessKey, secretKey, null) { }

    private void setDate(HttpWebRequest req, DateTime dateTime)
    {
        //string date = String.Format("{0:ddd,' 'dd' 'MMM' 'yyyy' 'HH':'mm':'ss' 'K}", dateTime);
        string date = dateTime.ToUniversalTime().ToString("r");
        MethodInfo priMethod = req.Headers.GetType().GetMethod("AddWithoutValidate", BindingFlags.Instance | BindingFlags.NonPublic);
        priMethod.Invoke(req.Headers, new[] {"Date", date});
    }

    public HttpWebRequest createWebRequest(string requestType, string bucketName, string objectName)
    {
        string requestUri = "http://" + bucketName + "." + target + "/" + objectName;
        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(requestUri);
        req.Method = requestType;
        return req;
    }

    public int GET(string bucketName, string objectName, string path)
    {
        if (path == null)
        {
            path = objectName;
        }

        HttpWebRequest req = createWebRequest("GET", bucketName, objectName);
        GCSAuthorization auth = new GCSAuthorization(this, req, bucketName, objectName);
        setDate(req, auth.dateTime);
        req.Headers.Add("Authorization: " + auth.ToString());

        HttpWebResponse res = (HttpWebResponse)req.GetResponse();

        long contentLength = res.ContentLength;

        BinaryReader reader = new BinaryReader(res.GetResponseStream());
        byte[] data = reader.ReadBytes((int)contentLength);
        reader.Close();

        File.WriteAllBytes(path, data);
        return (int)res.StatusCode;
    }

    public int PUT(string bucketName, string filePath)
    {
        string objectName = Path.GetFileName(filePath);

        FileInfo fi = new FileInfo(filePath);
        long contentLength = fi.Length;


        HttpWebRequest req = createWebRequest("PUT", bucketName, objectName);
        req.ContentType = "application/octet-stream";
        req.ContentLength = fi.Length;

        GCSAuthorization auth = new GCSAuthorization(this, req, bucketName, objectName);
        setDate(req, auth.dateTime);
        req.Headers.Add("Authorization: " + auth.ToString());

        BinaryWriter writer = new BinaryWriter(req.GetRequestStream());
        writer.Write(File.ReadAllBytes(filePath));
        writer.Close();

        HttpWebResponse res = (HttpWebResponse)req.GetResponse();
        return (int)res.StatusCode;
    }

    private class GCSAuthorization
    {
        private GCS gcs;
        private HttpWebRequest req;
        private string bucketName;
        private string objectName;
        public DateTime dateTime;

        public GCSAuthorization(GCS gcs, HttpWebRequest req, string bucketName, string objectName, DateTime dateTime)
        {
            this.gcs = gcs;
            this.req = req;
            this.bucketName = bucketName;
            this.objectName = objectName;
            this.dateTime = dateTime;
        }

        public GCSAuthorization(GCS gcs, HttpWebRequest req, string bucketName, string objectName):
            this(gcs, req, bucketName, objectName, DateTime.Now) { }

        // https://developers.google.com/storage/docs/migrating#authentication
        // "Expand to see the details of authentication"
        public string CreateMessageToBeSigned()
        {
            String newline = "\n";
            StringBuilder finalStr = new StringBuilder();
            finalStr.Append(req.Method);
            finalStr.Append(newline);
            // Content-MD5
            finalStr.Append(newline);
            finalStr.Append(req.ContentType);
            finalStr.Append(newline);
            finalStr.Append(dateTime.ToUniversalTime().ToString("r"));
            finalStr.Append(newline);
            finalStr.Append("/");
            finalStr.Append(bucketName);
            finalStr.Append("/");
            finalStr.Append(objectName);

            return finalStr.ToString();
        }

        public String CreateSignature()
        {
            String messageToBeSigned = CreateMessageToBeSigned();

            byte[] secretBytes = Encoding.UTF8.GetBytes(gcs.secretKey);
            byte[] messageBytes = Encoding.UTF8.GetBytes(messageToBeSigned);

            HMACSHA1 myhmacsha1 = new HMACSHA1(secretBytes);
            byte[] hashValue = myhmacsha1.ComputeHash(messageBytes);

            return Convert.ToBase64String(hashValue);
        }

        public override string ToString()
        {
            return "GOOG1 " + gcs.accessKey + ":" + CreateSignature();
        }
    }
}