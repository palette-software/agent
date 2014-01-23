using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Web;

public class HttpProcessor
{
    protected string host = "localhost";
    protected int port = 8888;
    protected NetworkStream stream = null;

    public HttpProcessor()
    {
    }

	public HttpProcessor(string host, int port)
	{
        this.host = host;
        this.port = port;
	}

    public void Connect()
    {
        IPAddress addr;
        GetResolvedConnectionIPAddress(host, out addr);  // FIXME: check return status
        
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        IPEndPoint remoteEP = new IPEndPoint(addr, port);
        socket.Connect(remoteEP);
        // FIXME: check for connected.  If not, throw exception ? //

        stream = new NetworkStream(socket, true);
    }

    public void Close()
    {
        if (stream != null)
        {
            stream.Close();
            stream = null;
        }
    }

    public void Run()
    {
        using (StreamReader reader = new StreamReader(stream))
        {
            HttpRequest req;
            while ((req = GetRequest(reader)) != null)
            {
                // FIXME: add a switch statement for GET/POST/PUT etc.
                Console.WriteLine(req.ToString());

                switch (req.method)
                {
                    case "GET":
                        //do this
                        break;
                    case "POST":
                    //do that
                        break;
                    case "PUT":
                        //do something else
                        break;
                }

                int statusCd = SendResponse(req, stream);
            }
            // FIXME: add try for closed sockets.
        }
    }

    protected HttpRequest GetRequest(StreamReader reader)
    {
        HttpRequest req = new HttpRequest();
        ParseRequestLine(req, reader.ReadLine());
        ReadHeaders(req, reader);

        // FIXME: handle closed sockets and/or bad requests.
        return req;
    }

    protected int SendResponse(HttpRequest req, NetworkStream stream)
    {
        using (System.IO.TextWriter writer = new StreamWriter(stream))
        {
            HttpResponse resp = new HttpResponse(writer);

            string body = "Version 0.0";

            try
            {
                resp.Write(body);

                resp.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }        
        return 0;
    }

    private void ParseRequestLine(HttpRequest req, string line)
    {
        string[] tokens = line.Split(' ');
        if (tokens.Length != 3)
        {
            throw new Exception("invalid http request line");
        }
        req.method = tokens[0].ToUpper();
        req.url = tokens[1];
        req.protocol_version = tokens[2];
    }

    private void ReadHeaders(HttpRequest req, StreamReader reader)
    {
        String line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Equals(""))
            {
                return;
            }

            int separator = line.IndexOf(':');
            if (separator == -1)
            {
                throw new Exception("invalid http header line: " + line);
            }
            String name = line.Substring(0, separator);
            int pos = separator + 1;
            while ((pos < line.Length) && (line[pos] == ' '))
            {
                pos++; // strip any spaces
            }

            string value = line.Substring(pos, line.Length - pos);
            req.headers[name] = value;
        }
    }

    // http://www.codeproject.com/Tips/440861/Resolving-a-hostname-in-Csharp-and-retrieving-IP-v
    public static bool GetResolvedConnectionIPAddress(string serverNameOrURL,
                   out IPAddress resolvedIPAddress)
    {
        bool isResolved = false;
        IPHostEntry hostEntry = null;
        IPAddress resolvIP = null;
        try
        {
            if (!IPAddress.TryParse(serverNameOrURL, out resolvIP))
            {
                hostEntry = Dns.GetHostEntry(serverNameOrURL);

                if (hostEntry != null && hostEntry.AddressList != null
                             && hostEntry.AddressList.Length > 0)
                {
                    if (hostEntry.AddressList.Length == 1)
                    {
                        resolvIP = hostEntry.AddressList[0];
                        isResolved = true;
                    }
                    else
                    {
                        foreach (IPAddress var in hostEntry.AddressList)
                        {
                            if (var.AddressFamily == AddressFamily.InterNetwork)
                            {
                                resolvIP = var;
                                isResolved = true;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                isResolved = true;
            }
        }
        catch
        {
            isResolved = false;
            resolvIP = null;
        }
        finally
        {
            resolvedIPAddress = resolvIP;
        }

        return isResolved;
    }
        
}
