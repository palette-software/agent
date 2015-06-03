using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;

public class ServerClient
{
    public const int PORT_HTTPS = 443;

    public const int DEFAULT_TIMEOUT = 120000; // 2 minutes

    public const string DEFAULT_HOST = "localhost";
    public const int DEFAULT_PORT = 22;

    public const string DEFAULT_CONNECT_HOST = "localhost";
    public const int DEFAULT_CONNECT_PORT = 888;

    public const string SECTION_NAME = "controller";

    public string Host;
    public int Port;
    public bool useSSL;
    public int TimeoutMilliseconds = 0; // infinite
    public bool Multiplex = false;

    // For SSL multiplexing
    public string ConnectHost;
    public int ConnectPort;

    public ServerClient() { }

    public void readConf(IniFile conf)
    {
        Host = conf.Read("host", SECTION_NAME, DEFAULT_HOST);
        Port = conf.ReadInt("port", SECTION_NAME, DEFAULT_PORT);
        useSSL = conf.ReadBool("ssl", SECTION_NAME, true);
        TimeoutMilliseconds = conf.ReadInt("timeout", SECTION_NAME, DEFAULT_TIMEOUT);
        if (conf.KeyExists("multiplex", SECTION_NAME))
        {
            Multiplex = conf.ReadBool("mutiplex", SECTION_NAME, false);
        }
        else
        {
            if (Port == PORT_HTTPS)
            {
                Multiplex = true;
            }
        }
        ConnectHost = conf.Read("connect-host", SECTION_NAME, DEFAULT_CONNECT_HOST);
        ConnectPort = conf.ReadInt("connect-port", SECTION_NAME, DEFAULT_CONNECT_PORT);
    }

    public Stream connect()
    {
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.ReceiveTimeout = TimeoutMilliseconds;
        socket.SendTimeout = TimeoutMilliseconds;

        /* resolve the remote address on each connection in case the IP changes. */
        IPAddress addr;
        NetUtil.GetResolvedConnectionIPAddress(Host, out addr);

        IPEndPoint remoteEP = new IPEndPoint(addr, Port);
        socket.Connect(remoteEP);

        Stream stream = new NetworkStream(socket, true);

        if (useSSL)
        {
            SslStream sslStream = new SslStream(stream, true, delegate { return true; });
            sslStream.AuthenticateAsClient(Host);
            stream = sslStream;
        }

        if (Multiplex)
        {
            ConnectRequest req = new ConnectRequest(ConnectHost, ConnectPort);
            ConnectResponse res = req.send(stream);
            if (res.StatusCode != (int)HttpStatusCode.OK)
            {
                throw new ConnectException(String.Format("Invalid CONNECT status code: {0}", res.StatusCode));
            }
        }
        return stream;
    }
}
