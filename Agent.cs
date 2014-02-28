using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Net;
using System.Threading;
using System.Configuration;
using log4net;
using log4net.Config;
using Microsoft.Win32;

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
    
    public const string DEFAULT_INSTALL_DIR = "c:/Palette";
    public const string DEFAULT_XID_SUBDIR = "XID";
    public const string DEFAULT_DATA_SUBDIR = "Data";
    public const string DEFAULT_DOCROOT_SUBDIR = "DocRoot";

    public IniFile conf = null;
    public string type;

    private bool _isArchiveAgent = false;
    public bool isArchiveAgent { get { return _isArchiveAgent; } }

    public string uuid = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX";
    public string hostname = "localhost";
    public string ipaddr;
    public string displayName = "unknown";

    public string controllerHost = Agent.DEFAULT_CONTROLLER_HOST;
    public int controllerPort = Agent.DEFAULT_CONTROLLER_PORT;
    public IPAddress controllerAddr;

    public string installDir = Agent.DEFAULT_INSTALL_DIR;
    public string xidDir;
    public string dataDir;

    public int archiveListenPort = Agent.DEFAULT_ARCHIVE_LISTEN_PORT;

    public ProcessManager processManager;
    protected MaintServer maintServer = null;

    // testing only.
    public string username = "palette";
    public string password = "unknown";

    //This has to be put in each class for logging purposes
    private static readonly log4net.ILog logger = log4net.LogManager.GetLogger
    (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="inifile">Path of .ini file</param>
    public Agent(string inifile)
    {
        log4net.Config.XmlConfigurator.Configure(); 

        type = Agent.TYPE;

        // set hostname (may be overridden by the INI file)
        hostname = Dns.GetHostName();

        displayName = Dns.GetHostName();

        conf = new IniFile(inifile);
        ParseIniFile();

        HttpProcessor.GetResolvedConnectionIPAddress(controllerHost, out controllerAddr);

        // FIXME: get path(s) from the INI file.
        xidDir = Path.Combine(this.installDir, Agent.DEFAULT_XID_SUBDIR);
        processManager = new ProcessManager(xidDir, type);

        dataDir = Path.Combine(this.installDir, Agent.DEFAULT_DATA_SUBDIR);
        ipaddr = GetFirstIPAddr();
    }

    /// <summary>
    /// Runs the HTTP Processing for a Primary or Worker Agent
    /// </summary>
    /// <returns>0 if process completes regularly</returns>
    public int Run()
    {
        string xidDir = Path.Combine(installDir, Agent.DEFAULT_XID_SUBDIR);

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
        
        FileServer fs = new FileServer(archiveListenPort, dataDir);
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
                logger.Error(exc.ToString());
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
    /// Finds out if Tableau is installed on local machine and if so returns version number
    /// otherwise returns null
    /// </summary>
    /// <returns>version number (i.e., "Tableau Server 8.1")</returns>
    public static string GetTableauVersion()
    {
        //Find out if Tableau is installed
        try
        {
            RegistryKey rk = Registry.LocalMachine.OpenSubKey("Software\\Tableau");
            string[] sk = rk.GetSubKeyNames();

            foreach (string key in sk)
            {
                if (key.Contains("Tableau Server")) return key;
            }
        }
        catch 
        {
            return null;
        }

        return null;
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

    protected string GetFirstIPAddr()
    {
        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily.ToString() == "InterNetwork")
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }

    public void startMaintServer()
    {
        string docroot = Path.Combine(installDir, Agent.DEFAULT_DOCROOT_SUBDIR);
        maintServer = new MaintServer(80, docroot);
        maintServer.Run();
    }

    public void stopMaintServer()
    {
        if (maintServer != null)
        {
            maintServer.Stop();
            maintServer.Close();
            maintServer = null;
        }
    }
}

//public class LogTest2
//{
//    //private static readonly ILog logger = LogManager.GetLogger(typeof(LogTest2));
//    private static readonly log4net.ILog logger = log4net.LogManager.GetLogger
//    (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

//    static LogTest2()
//    {
//        XmlConfigurator.Configure();
//    }

//    static void Log(string[] args)
//    {
//        logger.Debug("Here is a debug log.");
//        logger.Info("... and an Info log.");
//        logger.Warn("... and a warning.");
//        logger.Error("... and an error.");
//        logger.Fatal("... and a fatal error.");
//    }
//}


