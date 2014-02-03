﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Net;
using System.Threading;

public class Agent
{
    public const string VERSION = "0.0";
    public const string TYPE = "primary";
    public static string inifile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\agent.ini";

    public IniFile conf = null;
    public string type;

    public string uuid = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX";
    public string host = HttpProcessor.HOST;
    public int port = HttpProcessor.PORT;
    public IPAddress addr;

    public ProcessManager processManager;

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

        // FIXME: get path(s) from the INI file.
        processManager = new ProcessManager();
    }

    public int Run()
    {
        // FIXME: cleanup XID directory.
        PaletteHandler handler = new PaletteHandler(this);

        FileServer fs = new FileServer(8889);
        fs.Run();

        // FIXME: make this configurable in the INI file.
        int reconnectInterval = 10;

        while (true)
        {
            HttpProcessor processor = new HttpProcessor(host, port);

            try
            {
                processor.Connect();

                if (processor.isConnected)
                {
                    processor.Run(handler);
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
            }

            Thread.Sleep(reconnectInterval * 1000);
            processor.Close();
        }
        #if false
                        // FIXME: implement clean shutdown (currently unreachable).
                        processor.Close();
                        return 0;
        #endif

    }
}



