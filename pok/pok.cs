using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

class pok
{
    public static int TIMEOUT = 3000; /* 3secs */

    static int usage()
    {
        Console.Error.WriteLine("usage: pok <host-or-IP> <port>");
        return -1;
    }

    static int Main(string[] args)
    {
        //System.Diagnostics.Debugger.Launch();

        if (args.Length != 2)
        {
            return usage();
        }

        string server = args[0];
        int port;
        try {
            port = Convert.ToInt16(args[1]);
        } catch (Exception exc) 
        {
            Console.Error.WriteLine(exc.ToString());
            return -1;
        }

        IPAddress addr;
        bool success = NetUtil.GetResolvedConnectionIPAddress(server, out addr);
        if (!success)
        {
            return 1;
        }

        Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        IAsyncResult result = s.BeginConnect(addr, port, null, null);
        result.AsyncWaitHandle.WaitOne(pok.TIMEOUT, true);

        int returnCode = s.Connected ? 0 : 1;
        s.Close();

        return returnCode;
    }
}
