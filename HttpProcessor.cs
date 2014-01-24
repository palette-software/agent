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
    protected string ipaddress = null;

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

        this.ipaddress = addr.ToString();

        try
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint remoteEP = new IPEndPoint(addr, port);
            socket.Connect(remoteEP);         

            stream = new NetworkStream(socket, true);
        }
        catch (SocketException e)
        {
            Console.WriteLine("SocketException caught!");
            Console.WriteLine("Source : " + e.Source);
            Console.WriteLine("Message : " + e.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception caught!");
            Console.WriteLine("Source : " + e.Source);
            Console.WriteLine("Message : " + e.Message);
        }
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
        //cannot put two different types in same using statement but you can nest them
        using (StreamReader reader = new StreamReader(stream))         
        {            
            HttpRequest req;
            
            while ((req = GetRequest(reader)) != null)
            {
                // FIXME: add a switch statement for GET/POST/PUT etc.
                Console.WriteLine(req.ToString());

                using (System.IO.TextWriter writer = new StreamWriter(stream))
                {
                    HttpResponse res = new HttpResponse(writer);

                    switch (req.url)
                    {
                        case "/auth":
                            AuthReply reply = new AuthReply(@"/auth", "paul", "guessme", "00.00", 
                                this.ipaddress, this.host, this.port.ToString());
                            reply.SerializeToJSON();
                            res.Write(reply.ToString());
                            break;
                        case "/status":

                            break;
                        default:
                            //response should send HTTP not found error (404 error)
                            res.StatusCode = 404;          
                            break;
                    }

                    res.Flush();
                }
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
