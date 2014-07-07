﻿using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Web;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using log4net;
using log4net.Config;

/// <summary>
/// Encapsulates the socket connections and network stream to handle HTTP requests 
/// </summary>
public class HttpProcessor
{
    protected string host;
    protected int port;
    protected bool ssl = false;
    protected int timeout = 0; // infinite
    protected Stream stream = null;
    protected string ipaddress = null;
    public bool isConnected = false;

    //This has to be put in each class for logging purposes
    private static readonly log4net.ILog logger = log4net.LogManager.GetLogger
    (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="host">host name (i.e. "localhost")</param>
    /// <param name="port">port (i.e. "8080")</param>
	public HttpProcessor(string host, int port)
	{
        this.host = host;
        this.port = port;
	}

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="host">host name (i.e. "localhost")</param>
    /// <param name="port">port (i.e. "8080")</param>
    /// <param name="ssl">use ssl</param>
    public HttpProcessor(string host, int port, bool ssl) : this(host, port)
    {
        this.ssl = ssl;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="host">host name (i.e. "localhost")</param>
    /// <param name="port">port (i.e. "8080")</param>
    /// <param name="ssl">use ssl</param>
    /// <param name="timeout">socket send/recv timeout in milliseconds</param>
    public HttpProcessor(string host, int port, bool ssl, int timeout)
        : this(host, port, ssl)
    {
        this.timeout = timeout;
    }

    /// <summary>
    /// Connects a socket to a specified host
    /// </summary>
    public void Connect()
    {
        IPAddress addr;
        GetResolvedConnectionIPAddress(host, out addr);  // FIXME: check return status

        this.ipaddress = addr.ToString();

        try
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.ReceiveTimeout = timeout;
            socket.SendTimeout = timeout;

            IPEndPoint remoteEP = new IPEndPoint(addr, port);
            socket.Connect(remoteEP);         

            stream = new NetworkStream(socket, true);

            if (ssl)
            {
                SslStream sslStream = new SslStream(stream, true, CertificateValidationCallback);
                sslStream.AuthenticateAsClient(host);
                stream = sslStream;
            }

            isConnected = true;
        }
        catch (Exception e)
        {
            isConnected = false;
            throw e;
        }
    }

    /// <summary>
    /// Closes the connection stream
    /// </summary>
    public void Close()
    {
        if (stream != null)
        {
            stream.Close();
            stream = null;
        }
    }

    /// <summary>
    /// Loops infintely to handle incoming HTTP requests
    /// </summary>
    /// <param name="handler">HttpHandler</param>
    public void Run(HttpHandler handler)
    {
        for ( ; ; )  //Loop infinitely here
        {
            //cannot put two different types in same using statement but you can nest them
            using (StreamWriter writer = new StreamWriter(stream))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    HttpRequest req;

                    while ((req = HttpRequest.Get(reader)) != null)
                    {
                        logger.Debug(req.ToString());

                        // This could be cleaner.
                        HttpResponse res = new HttpResponse(writer);
                        req.Response = res;

                        try
                        {
                            res = handler.Handle(req);
                        }
                        catch (HttpBadRequest exc)
                        {
                            // forcible close the socket.
                            if (exc.Body != null)
                            {
                                logger.Error(exc.ToString());
                            }
                            else
                            {
                                logger.Error(exc.ToString());
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
                        catch (Exception exc)
                        {
                            res.StatusCode = 500;
                            res.StatusDescription = "Internal Server Error";
                            res.Write(exc.ToString() + "\r\n");
                            res.Write(exc.StackTrace);
                            // FIXME: log this.
                        }
#if False
                        catch (Exception exc)
                        {
                            logger.Error("General Exception: " + exc.Message + " Source: " + exc.Source);
                        }
#endif

                        res.Flush();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gets the resolved ip address for a specified server name or url.  See 
    /// http://www.codeproject.com/Tips/440861/Resolving-a-hostname-in-Csharp-and-retrieving-IP-v
    /// </summary>
    /// <param name="serverNameOrURL">Server name or URL</param>
    /// <param name="resolvedIPAddress">Out: the resolved ip address</param>
    /// <returns></returns>
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

    static bool CertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        // FIXME
        return true;
    }
}
