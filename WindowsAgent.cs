using System;
using System.IO;
using System.Reflection;
using System.Net;

public class Agent
{
    public const string VERSION = "0.0";
    public const string TYPE = "primary";

    public IniFile conf = null;
    public string type;

    public string uuid = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX";
    public string host = HttpProcessor.HOST;
    public int port = HttpProcessor.PORT;
    public IPAddress addr;

    // testing only.
    public string username = "palette";
    public string password = "unknown";

    public Agent(string inifile)
    {
        type = Agent.TYPE;
        conf = new IniFile(inifile);

        if (conf.KeyExists("uuid", "DEFAULT"))
        {
            uuid = conf.Read("uuid", "DEFAULT");
        }

        if (conf.KeyExists("host", "controller"))
        {
            host = conf.Read("host", "controller");
        }

        if (conf.KeyExists("port", "controller")) 
        {
            port = Convert.ToInt16(conf.Read("port", "controller"));
        }

        HttpProcessor.GetResolvedConnectionIPAddress(host, out addr);
    }

    public static string SendResponse(HttpListenerRequest request)
    {
        return string.Format("<HTML><BODY>Pallete Windows Agent: {0}</BODY></HTML>", Agent.VERSION);
    }

    public static int Main(String[] args)
    {
        string inifile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\agent.ini";

        if (args.Length == 1)
        {
            inifile = args[1];
        }
        else if (args.Length != 0)
        {
            Console.WriteLine("usage: %s [inifile]", args[0]);
            return -1;
        }

        Agent agent = new Agent(inifile);

        // FIXME: cleanup XID directory.
        PaletteHandler handler = new PaletteHandler(agent);

        WebServer ws = new WebServer(SendResponse, "http://localhost:8889/");
        ws.Run();

        HttpProcessor processor = new HttpProcessor(agent.host, agent.port);
        processor.Connect();

        if (processor.isConnected)
        {
            processor.Run(handler);
            processor.Close();
            return 0;
        }
        else
        {
            return -1;
        }        
    }
}



