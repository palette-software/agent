﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Net;
using System.Threading;

/// <summary>
/// The class that needs to be instantiated by a Console app or Windows service 
/// to run an agent
/// </summary>
public class Agent
{
    public const string VERSION = "0.0";
    public const string TYPE = "primary";

    public const string DEFAULT_SECTION = "DEFAULT";

    public const string DEFAULT_CONTROLLER_HOST = "localhost";
    public const int DEFAULT_CONTROLLER_PORT = 8888;
    public const int DEFAULT_ARCHIVE_LISTEN_PORT = 8889;
    
    public const string DEFAULT_INSTALL_DIR = "C:\\Palette";
    public const string DEFAULT_DOCUMENT_SUBDIR = "Data";
    public const string DEFAULT_XID_SUBDIR = "XID";

    public IniFile conf = null;
    public string type;

    private bool _isArchiveAgent = false;
    public bool isArchiveAgent { get { return _isArchiveAgent; } }

    public string uuid = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX";
    public string hostname = "localhost";

    public string controllerHost = Agent.DEFAULT_CONTROLLER_HOST;
    public int controllerPort = Agent.DEFAULT_CONTROLLER_PORT;
    public IPAddress controllerAddr;

    public string installDir = Agent.DEFAULT_INSTALL_DIR;

    public int archiveListenPort = Agent.DEFAULT_ARCHIVE_LISTEN_PORT;

    public ProcessManager processManager;

    // testing only.
    public string username = "palette";
    public string password = "unknown";

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="inifile">Path of .ini file</param>
    public Agent(string inifile)
    {
        type = Agent.TYPE;
        conf = new IniFile(inifile);
        ParseIniFile();

        HttpProcessor.GetResolvedConnectionIPAddress(controllerHost, out controllerAddr);

        // FIXME: get path(s) from the INI file.
        string xidDir = Path.Combine(this.installDir, Agent.DEFAULT_XID_SUBDIR);
        processManager = new ProcessManager(xidDir);
    }

    /// <summary>
    /// Runs the HTTP Processing for a Primary or Worker Agent
    /// </summary>
    /// <returns>0 if process completes regularly</returns>
    public int Run()
    {
        string xidDir = Path.Combine(this.installDir, Agent.DEFAULT_XID_SUBDIR);

        System.IO.DirectoryInfo xidContents = new DirectoryInfo(xidDir);

        foreach (FileInfo file in xidContents.GetFiles())
        {
            file.Delete();
        }
        foreach (DirectoryInfo dir in xidContents.GetDirectories())
        {
            dir.Delete(true);
        }
        
        PaletteHandler handler = new PaletteHandler(this);
        
        FileServer fs = new FileServer(archiveListenPort, xidDir);
        fs.Run();

        // FIXME: make this configurable in the INI file.
        int reconnectInterval = 10;

        while (true)
        {
            HttpProcessor processor = new HttpProcessor(controllerHost, controllerPort);

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

    /// <summary>
    /// Parses text in .ini file
    /// </summary>
    private void ParseIniFile()
    {
        if (conf.KeyExists("type", DEFAULT_SECTION))
        {
            type = conf.Read("type", DEFAULT_SECTION);
        }
        type = type.ToLower();
        if (type != "primary" && type != "worker" && type != "other")
        {
            throw new ArgumentException("DEFAULT:type");
        }

        if (conf.KeyExists("uuid", DEFAULT_SECTION))
        {
            uuid = conf.Read("uuid", DEFAULT_SECTION);
        }

        if (conf.KeyExists("hostname", DEFAULT_SECTION))
        {
            hostname = conf.Read("hostname", DEFAULT_SECTION);
        }

        if (conf.KeyExists("install-dir", DEFAULT_SECTION))
        {
            installDir = conf.Read("install-dir", DEFAULT_SECTION);
        }

        if (conf.KeyExists("archive", DEFAULT_SECTION))
        {
            string archive = conf.Read("archive", DEFAULT_SECTION).ToUpper();
            if (archive == "TRUE")
            {
                _isArchiveAgent = true;
            }
        }

        if (conf.KeyExists("host", "controller"))
        {
            controllerHost = conf.Read("host", "controller");
        }

        if (conf.KeyExists("port", "controller"))
        {
            controllerPort = Convert.ToInt16(conf.Read("port", "controller"));
        }

        if (conf.KeyExists("listen-port", "archive"))
        {
            archiveListenPort = Convert.ToInt16(conf.Read("listen-port", "archive"));
        }
    }
}



