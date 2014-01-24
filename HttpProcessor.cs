using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Web;

public class HttpProcessor
{
    public const string HOST = "localhost";
    public const int PORT = 8888;

    protected string host = HOST;
    protected int port = PORT;
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
            throw e;
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception caught!");
            Console.WriteLine("Source : " + e.Source);
            Console.WriteLine("Message : " + e.Message);
            throw e;
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

    public void Run(HttpHandler handler)
    {
        //cannot put two different types in same using statement but you can nest them
        using (StreamReader reader = new StreamReader(stream))
        {
            using (StreamWriter writer = new StreamWriter(stream))
            {
                HttpRequest req;
            
                while ((req = HttpRequest.Get(reader)) != null)
                {                 
                    Console.WriteLine(req.ToString());

                    // This could be cleaner.
                    HttpResponse res = new HttpResponse(writer);
                    req.Response = res;

                    try
                    {
                        res = handler.handle(req);
                    }
                    catch (HttpBadRequest exc)
                    {
                        // forcible close the socket.
                        if (exc.Body != null)
                        {
                            Console.WriteLine(exc.Body);
                        }
                        else
                        {
                            Console.WriteLine(exc.ToString());
                        }
                        return;
                    }
                    catch (HttpException exc)
                    {
                        res.StatusCode = exc.StatusCode;
                        res.StatusDescription = exc.Reason;
                        if (exc.Body != null)
                        {
                            res.Write(exc.Body);
                        }
                    }

                    res.Flush();
                }
            }            

            // FIXME: add try for closed sockets.
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
