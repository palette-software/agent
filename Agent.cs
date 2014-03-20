using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Net;
using System.Threading;
using System.Configuration;
using log4net;
using log4net.Repository.Hierarchy;
using log4net.Core;
using log4net.Appender;
using log4net.Layout;
//using log4net.Config;
using Microsoft.Win32;

/// <summary>
/// The class that needs to be instantiated by a Console app or Windows service 
/// to run an agent
/// </summary>
public class Agent
{
    public const string VERSION = "0.1";
    public const string TYPE = "primary";

    public const string DEFAULT_SECTION = "DEFAULT";
    public const string DEFAULT_CONTROLLER_HOST = "localhost";
    public const int DEFAULT_CONTROLLER_PORT = 8888;
    public const int DEFAULT_ARCHIVE_LISTEN_PORT = 8889;

    public const string DEFAULT_MAX_LOG_SIZE = "10MB"; 

    public IniFile conf = null;
    public string type;  //Agent type (primary, worker, other, etc.)

    private bool _isArchiveAgent = false;
    public bool isArchiveAgent { get { return _isArchiveAgent; } }

    public string uuid = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX";
    public string hostname = "localhost";
    public string ipaddr;
    public string displayName = "unknown";

    public string controllerHost = Agent.DEFAULT_CONTROLLER_HOST;
    public int controllerPort = Agent.DEFAULT_CONTROLLER_PORT;
    public IPAddress controllerAddr;

    public string installDir;
    public string binDir;
    public string xidDir;
    public string dataDir;
    public string iniDir;
    public string logName;
    public string docRoot;
    public string maxLogSize = Agent.DEFAULT_MAX_LOG_SIZE;

    public int archiveListenPort = Agent.DEFAULT_ARCHIVE_LISTEN_PORT;

    public ProcessManager processManager;
    protected MaintServer maintServer = null;

    // testing only.
    public string username = "palette";
    public string password = "unknown";

    public string tableauVersion = "?";

    //This has to be put in each class for logging purposes
    private static readonly log4net.ILog logger = log4net.LogManager.GetLogger
    (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="inifile">Path of .ini file</param>
    /// <param name="runAsService">True if run as a Windows Service, False as Console App</param>
    public Agent(string inifile, bool runAsService)
    {
        //Set some Agent defaults (may be overridden by the INI file)
        type = Agent.TYPE;
        hostname = Dns.GetHostName();
        displayName = Dns.GetHostName();        

        if (File.Exists(inifile))
        {           
            conf = new IniFile(inifile);
            ParseIniFile();
        }

        binDir = Path.Combine(installDir, "bin");  
        xidDir = Path.Combine(installDir, "XID");
        dataDir = Path.Combine(installDir, "Data");
        docRoot = Path.Combine(installDir, "DocRoot");
        logName = Path.Combine(installDir, "log\\agent.log");

        SetupLogging(runAsService);

        if (File.Exists(inifile)) 
        {
            logger.Info("Starting Agent using inifile: " + inifile);
        }
        else
        {
            logger.Error("agent.ini file not found: " + inifile);
            logger.Error("Starting Agent with default settings");
        }

        HttpProcessor.GetResolvedConnectionIPAddress(controllerHost, out controllerAddr);

        processManager = new ProcessManager(xidDir, binDir, type);

        ipaddr = GetFirstIPAddr();
    }

    /// <summary>
    /// Takes configuration settings from .ini file to setup logging options
    /// </summary>
    public void SetupLogging(bool runAsService)
    {
        Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();

        PatternLayout patternLayout = new PatternLayout();
        patternLayout.ConversionPattern = "%date [%thread] %level %logger - %message%newline";
        patternLayout.ActivateOptions();

        RollingFileAppender roller = new RollingFileAppender();
        roller.AppendToFile = true;
        roller.File = logName;
        roller.Layout = patternLayout;
        roller.MaxSizeRollBackups = 5;
        roller.MaximumFileSize = maxLogSize;
        roller.RollingStyle = RollingFileAppender.RollingMode.Size;
        roller.StaticLogFileName = true;
        roller.LockingModel = new FileAppender.MinimalLock();
        roller.ActivateOptions();
        hierarchy.Root.AddAppender(roller);

        if (!runAsService)
        {
            ConsoleAppender console = new ConsoleAppender();
            console.Layout = patternLayout;
            console.ActivateOptions();
            hierarchy.Root.AddAppender(console);
        }

        hierarchy.Root.Level = Level.Info;
        hierarchy.Configured = true;
    }

    /// <summary>
    /// Runs the HTTP Processing for a Primary or Worker Agent
    /// </summary>
    /// <returns>0 if process completes regularly</returns>
    public int Run()
    {
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
            //First try Registry
            RegistryKey rk = Registry.LocalMachine.OpenSubKey("Software\\Tableau");
            string[] sk = rk.GetSubKeyNames();

            foreach (string key in sk)
            {
                if (key.Contains("Tableau Server")) return key;
            }

            //Local machine may not allow registry access from a Service, so look for folder also
            FileInfo f = new FileInfo(Assembly.GetExecutingAssembly().Location);
            string tableauFolder = Path.Combine(Path.GetPathRoot(f.FullName), "Program Files\\Tableau\\Tableau Server");
            string[] subFolders = null;
            if (Directory.Exists(tableauFolder))
            {
                subFolders = Directory.GetDirectories(tableauFolder);
                string[] tokens = subFolders[0].ToString().Split('\\');
                return tokens[tokens.Length - 1];
            }
            return null;
        }
        catch 
        {
            return null;
        }
    }

    public void startMaintServer()
    {
        maintServer = new MaintServer(80, docRoot);
        maintServer.Run();
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

        if (conf.KeyExists("location", "logging"))
        {
            logName = conf.Read("location", "logging");
        }

        if (conf.KeyExists("maxsize", "logging"))
        {
            maxLogSize = conf.Read("maxsize", "logging");
        }
    }

    /// <summary>
    /// Finds out if Tableau is installed on local machine and if so returns version number
    /// otherwise returns null
    /// </summary>
    /// <returns>version number (i.e., "Tableau Server 8.1")</returns>
    public static string GetTableauPath()
    {
        //Find out if Tableau is installed
        try
        {
            RegistryKey rk = Registry.LocalMachine.OpenSubKey("Software\\Tableau");
            string[] sk = rk.GetSubKeyNames();

            foreach (string key in sk)
            {
                if (key.Contains("Tableau Server"))
                {
                    RegistryKey ssk = Registry.LocalMachine.OpenSubKey("Software\\Tableau\\" + key
                        + "\\Directories\\");
                    return ssk.GetValue("AppVersion").ToString();
                }
            }
        }
        catch
        {
            return null;
        }

        return null;
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

    public void stopMaintServer()
    {
        if (maintServer != null)
        {
            maintServer.Stop();
            maintServer.Close();
            maintServer = null;
        }
    }

    /// <summary>
    /// The function below will return the x86 Program Files directory in all of these three Windows configurations:
    ///   32 bit Windows, 32 bit program running on 64 bit Windows, 64 bit program running on 64 bit windows
    /// </summary>
    /// <returns>x86 Program Files directory</returns>
    public static string ProgramFilesx86()
    {
        if (8 == IntPtr.Size
            || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
        {
            return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
        }

        return Environment.GetEnvironmentVariable("ProgramFiles");
    }
}

