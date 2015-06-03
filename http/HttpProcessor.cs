using System;
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
    protected ServerClient client;
    protected Stream stream = null;

    public bool isConnected = false; // FIXME: add a getter.
    
    //This has to be put in each class for logging purposes
    private static readonly log4net.ILog logger = log4net.LogManager.GetLogger
    (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public HttpProcessor(ServerClient client)
	{
        this.client = client;
	}

    /// <summary>
    /// Connects a socket to a specified host
    /// </summary>
    public void Connect()
    {
        try
        {
            stream = client.connect();
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
    /// Continuously read HTTP requests as long as the socket is open.
    /// </summary>
    /// <param name="handler">HttpHandler</param>
    public void Run(HttpHandler handler)
    {
        //cannot put two different types in same using statement but you can nest them
        using (HttpStreamWriter writer = new HttpStreamWriter(stream))
        {
            using (HttpStreamReader reader = new HttpStreamReader(stream))
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
                    catch (HttpException exc)
                    {
                        logger.Error(exc.ToString());

                        res.StatusCode = exc.StatusCode;
                        res.StatusDescription = exc.Reason;
                        if (exc.Body != null)
                        {
                            logger.Error(exc.Body);
                            res.Write(exc.Body);
                        }
                    }
                    catch (Exception exc)
                    {
                        res.StatusCode = 500;
                        res.StatusDescription = "Internal Server Error";
                        res.Write(exc.ToString() + "\r\n");
                        res.Write(exc.StackTrace);
                        res.needClose = true;

                        logger.Error("500 Internal Server Error");
                        logger.Error(exc.ToString());
                        logger.Error(exc.StackTrace);
                    }

                    res.Flush();

                    // FIXME: find a cleaner way to shutdown the agent.
                    if (res.needRestart)
                    {
                        stream.Close();
                        Environment.Exit(0);
                    }

                    if (res.needClose)
                    {
                        stream.Close();
                        return;
                    }
                }
            }
        }
    }

    static bool CertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        // FIXME
        return true;
    }
}
