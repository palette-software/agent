﻿using System;
using System.Net;
using System.Threading;
using System.Linq;
using System.Text;

/// <summary>
/// Base Class for a Standard Web Server
///   see: http://www.codehosting.net/blog/BlogEngine/post/Simple-C-Web-Server.aspx
/// All the work is done on background threads, which will be automatically cleaned up when the program quits.
/// </summary>
public abstract class HttpBaseServer
{
        protected readonly HttpListener listener = new HttpListener();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="prefixes">URI Prefixes (i.e. "http://localhost:8080/index/")</param>
        public HttpBaseServer(string[] prefixes)
        {
            if (!HttpListener.IsSupported)
                throw new NotSupportedException(
                    "Needs Windows XP SP2, Server 2003 or later.");
 
            // URI prefixes are required, for example 
            // "http://localhost:8080/index/".
            if (prefixes == null)
                throw new ArgumentException("prefixes");
            
            foreach (string s in prefixes)
                listener.Prefixes.Add(s);
        }
 
        public HttpBaseServer() : this(new string[0]) { }
 
        /// <summary>
        /// Starts the listener
        /// </summary>
        public void Run()
        {
            listener.Start();

            ThreadPool.QueueUserWorkItem((o) =>
            {
                foreach (string prefix in listener.Prefixes)
                    Console.WriteLine(prefix + " ...");
                try
                {
                    while (listener.IsListening)
                    {
                        ThreadPool.QueueUserWorkItem((c) =>
                        {
                            var ctx = c as HttpListenerContext;
                            try
                            {
                                Handle(ctx);
                            }
                            catch (HttpException exc)
                            {
                                ctx.Response.StatusCode = exc.StatusCode;
                                ctx.Response.StatusDescription = exc.Reason;                                
                            }
                            catch (Exception exc)
                            {
                                ctx.Response.StatusCode = 100;
                                ctx.Response.StatusDescription = "Internal Server Error";   
                                Console.WriteLine(exc.ToString());
                            }
                            finally
                            {
                                // always close the stream
                                ctx.Response.OutputStream.Close();
                                ctx.Response.Close();
                            }
                        }, listener.GetContext());
                    }
                }
                catch { } // suppress any exceptions
            });
        }

        /// <summary>
        /// Sends buffered response
        /// </summary>
        /// <param name="res">HttpListenerResponse</param>
        /// <param name="rstr">Response string</param>
        protected void SendString(HttpListenerResponse res, string rstr)
        {
            byte[] buf = Encoding.UTF8.GetBytes(rstr);
            res.ContentLength64 = buf.Length;
            res.OutputStream.Write(buf, 0, buf.Length);
        }
 
        /// <summary>
        /// Stops the listener
        /// </summary>
        public void Stop()
        {
            listener.Stop();
        }

        /// <summary>
        /// Closes the listener
        /// </summary>
        public void Close()
        {
            listener.Close();
        }

        protected abstract void Handle(HttpListenerContext ctx);
}

#if False
class Program
{
    static void Main(string[] args)
    {
        WebServer ws = new WebServer(SendResponse, "http://localhost:8080/test/");
        ws.Run();
        Console.WriteLine("A simple webserver. Press a key to quit.");
        Console.ReadKey();
        ws.Stop();
    }
 
    public static string SendResponse(HttpListenerRequest request)
    {
        return string.Format("<HTML><BODY>My web page.<br>{0}</BODY></HTML>", DateTime.Now);    
    }
}
#endif