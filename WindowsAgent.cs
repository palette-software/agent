using System;
using System.IO;
using System.Reflection;

public class Agent
{
    public const string VERSION = "0.0";

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

        IniFile conf = new IniFile(inifile);
        string host = conf.KeyExists("host", "controller") ? conf.Read("host", "controller") : HttpProcessor.HOST;
        int port = conf.KeyExists("port", "controller") ? Convert.ToInt16(conf.Read("port", "controller")) : HttpProcessor.PORT;

        string uuid = conf.KeyExists("uuid", "DEFAULT") ? conf.Read("uuid", "DEFAULT") : "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX";

        PaletteHandler handler = new PaletteHandler(uuid);

        HttpProcessor processor = new HttpProcessor(host, port);
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



