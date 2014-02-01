using System;
using System.Net;
using System.Threading;
using System.Linq;
using System.Text;
 
// Standard Web Server class - see: http://www.codehosting.net/blog/BlogEngine/post/Simple-C-Web-Server.aspx
// All the work is done on background threads, which will be automatically cleaned up when the program quits.
public class WebServer
{
        private readonly HttpListener listener = new HttpListener();
        private readonly Func<HttpListenerRequest, string> responderMethod;
 
        public WebServer(string[] prefixes, Func<HttpListenerRequest, string> method)
        {
            if (!HttpListener.IsSupported)
                throw new NotSupportedException(
                    "Needs Windows XP SP2, Server 2003 or later.");
 
            // URI prefixes are required, for example 
            // "http://localhost:8080/index/".
            if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("prefixes");
 
            // A responder method is required
            if (method == null)
                throw new ArgumentException("method");
 
            foreach (string s in prefixes)
                listener.Prefixes.Add(s);
            responderMethod = method;
            listener.Start();
        }
 
        public WebServer(Func<HttpListenerRequest, string> method, params string[] prefixes)
            : this(prefixes, method) { }
 
        public void Run()
        {
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
                                string rstr = responderMethod(ctx.Request);
                                byte[] buf = Encoding.UTF8.GetBytes(rstr);
                                ctx.Response.ContentLength64 = buf.Length;
                                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                            }
                            catch { } // suppress any exceptions
                            finally
                            {
                                // always close the stream
                                ctx.Response.OutputStream.Close();
                            }
                        }, listener.GetContext());
                    }
                }
                catch { } // suppress any exceptions
            });
        }
 
        public void Stop()
        {
            listener.Stop();
            listener.Close();
        }
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