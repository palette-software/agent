using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

/// <summary>
/// A CONNECT request is really just HTTP, where we control the SockStream.
/// </summary>
class ConnectRequest
{
    /*
     * Apache2 always responds with HTTP/1.0 regardless, so start with that.
     */
    public const string PROTOCOL = "HTTP/1.0";
    public string Hostname;
    public int Port;

    public ConnectRequest(string hostname, int port)
    {
        Hostname = hostname;
        Port = port;
    }

    public ConnectResponse send(Stream stream)
    {
        using (HttpStreamWriter writer = new HttpStreamWriter(stream, false))
        {
            string req = String.Format("CONNECT {0}:{1} {2}", Hostname, Port, PROTOCOL);
            writer.WriteLine(req);
            writer.WriteLine();
        }

        ConnectResponse res = new ConnectResponse();

        using (HttpStreamReader reader = new HttpStreamReader(stream, false))
        {
            res.readStatusLine(reader);
            res.readHeaders(reader);
            if (res.ContentLength > 0)
            {
                // The server sent and error description in the body.
                res.Body = reader.ReadText(res.ContentLength);
            }
        }
        return res;
    }
}

internal class ConnectResponse
{
    public int StatusCode;
    public string StatusDescription;
    public int ContentLength = 0;
    public Dictionary<string, string> Headers = new Dictionary<string, string>();
    public string Body = null;

    internal void readStatusLine(HttpStreamReader reader)
    {
        string line = reader.ReadLine();
        string [] tokens = line.Split(" ".ToCharArray(), 3);
        if (tokens.Length != 3)
        {
            throw new ConnectException("Malformed status line: " + line);
        }

        string protocol = tokens[0].ToUpper();
        if (protocol != ConnectRequest.PROTOCOL)
        {
            throw new ConnectException("Invalid HTTP protocol version: " + line);
        }

        try
        {
            StatusCode = Convert.ToInt32(tokens[1]);
        }
        catch (Exception)
        {
            throw new ConnectException("Invalid HTTP status code: " + line);
        }

        StatusDescription = tokens[2].Trim();
    }

    internal void readHeaders(HttpStreamReader reader)
    {
        string line;
        while ((line = reader.ReadLine()) != "")
        {
            string[] tokens = line.Split(":".ToCharArray(), 2);
            if (tokens.Length != 2)
            {
                throw new ConnectException("Invalid HTTP header: " + line);
            }
            string name = tokens[0].Trim().ToLower();
            string value = tokens[1].Trim();

            Headers[name] = value;

            if (name == "content-length")
            {
                try {
                    ContentLength = Convert.ToInt32(value);
                } catch (Exception) {
                    throw new ConnectException("Invalid Content-Length: " + value);
                }
            }
        }
    }
}

internal class ConnectException : Exception
{
    public ConnectException(string message) : base(message) { }
}