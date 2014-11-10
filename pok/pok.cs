using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

class pok
{
    public static int TIMEOUT = 3000; /* 3secs */

    static int usage()
    {
        Console.Error.WriteLine("usage: pok <host-or-IP> <port> [timeout]");
        Console.Error.WriteLine("  'timeout' defaults to " + pok.TIMEOUT.ToString() + " milliseconds.");
        return -1;
    }

    static int Main(string[] args)
    {
        //System.Diagnostics.Debugger.Launch();

        if (args.Length < 2 || args.Length > 3)
        {
            return usage();
        }

        string server = args[0];
        int port;
        try
        {
            port = Convert.ToInt16(args[1]);
        } catch (Exception exc) 
        {
            Console.Error.WriteLine(exc.ToString());
            return -1;
        }

        int timeout = pok.TIMEOUT;
        if (args.Length > 2)
        {
            try
            {
                timeout = Convert.ToInt32(args[2]);
            }
            catch (Exception exc)
            {
                Console.Error.WriteLine(exc.ToString());
                return -1;
            }
        }
        
        Dictionary<string, object> data = new Dictionary<string,object>();

        Stopwatch sw = new Stopwatch();
        sw.Start();

        IPAddress addr;
        bool success = NetUtil.GetResolvedConnectionIPAddress(server, out addr);
        if (!success)
        {
            sw.Stop();
            data["status"] = "FAILED";
            data["error"] = "Failed to resolve address: " + server;
            data["milliseconds"] = sw.ElapsedMilliseconds;
            Console.WriteLine(fastJSON.JSON.Instance.ToJSON(data));
            return 1;
        }

        Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        IAsyncResult result = s.BeginConnect(addr, port, null, null);
        result.AsyncWaitHandle.WaitOne(timeout, true);
        sw.Stop();

        int returnCode = s.Connected ? 0 : 1;
        s.Close();

        data["ip"] = addr.ToString();
        data["milliseconds"] = sw.ElapsedMilliseconds;
        if (returnCode == 0)
        {
            data["status"] = "OK";
        }
        else
        {
            data["status"] = "FAILED";
            if (!result.IsCompleted)
            {
                data["error"] = "Timeout";
            }
            else
            {
                data["error"] = "Unknown connection error";
            }
        }

        string json = fastJSON.JSON.Instance.ToJSON(data);
        Console.WriteLine(json);
        return returnCode;
    }
}
