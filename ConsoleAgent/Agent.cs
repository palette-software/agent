using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Configuration;
using System.Collections.Generic;
using log4net;
using log4net.Repository.Hierarchy;
using log4net.Core;
using log4net.Appender;
using log4net.Layout;
using Microsoft.Win32;

/// <summary>
/// The class that needs to be instantiated by a Console app or Windows service 
/// to run an agent
/// </summary>
public class Agent : Base
{
    public const string DEFAULT_SECTION = "DEFAULT";
    public const string DEFAULT_CONTROLLER_HOST = "localhost";
    public const int DEFAULT_CONTROLLER_PORT = 8888;
    public const int DEFAULT_ARCHIVE_PORT = 8889;
    public const int DEFAULT_TIMEOUT = 120000; // 2 minutes

    public IniFile conf = null;

    public string uuid;
    public string hostname = "localhost";
    public string ipaddr;

    // Values to pre-pend to the environment variable 'PATH' for child processes.
    public string envPath;

    public string controllerHost;
    public int controllerPort;
    public IPAddress controllerAddr;
    public bool controllerSsl;
    public int controllerTimeoutMilliseconds;

    public string installDir;
    public string programDataDir;
    public string xidDir;

    public int archivePort;
    private Apache2 archiveServer;

    private Apache2 maintServer;
    
    public ProcessManager processManager;

    public string licenseKey;
    
    //This has to be put in each class for logging purposes
    private static readonly log4net.ILog logger = log4net.LogManager.GetLogger
    (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="inifile">Path of .ini file</param>
    /// <param name="runAsService">True if run as a Windows Service, False as Console App</param>
    public Agent(string inifile)
    {
        //Set some Agent defaults (may be overridden by the INI file)
        hostname = Dns.GetHostName();

        conf = new IniFile(inifile);
        ParseIniFile();

        if (!Directory.Exists(installDir))
        {
            throw new DirectoryNotFoundException(installDir);
        }

        /* If these calls throw an exception then exit since nothing can be done. */
        Directory.CreateDirectory(programDataDir);

        xidDir = StdPath.Combine(programDataDir, "XID");
        Directory.CreateDirectory(programDataDir);

        SetupLogging();

        if (File.Exists(inifile)) 
        {
            logger.Info("Starting Agent using inifile: " + inifile);
        }
        else
        {
            logger.Error("agent.ini file not found: " + inifile);
            logger.Error("Starting Agent with default settings");
        }

        NetUtil.GetResolvedConnectionIPAddress(controllerHost, out controllerAddr);

        processManager = new ProcessManager(xidDir, installDir, envPath);

        ipaddr = GetFirstIPAddr();

        string path = StdPath.Combine(installDir, "maint", "conf", "httpd.conf");
        string logDir = StdPath.Combine(programDataDir, "logs", "maint");
        maintServer = new Apache2(MAINTENANCE_SERVICE_NAME, path, installDir, logDir);

        path = StdPath.Combine(installDir, "conf", "archive", "httpd.conf");
        logDir = StdPath.Combine(programDataDir, "logs", "archive");
        archiveServer = new Apache2(ARCHIVE_SERVICE_NAME, path, installDir, logDir);
    }

    /// <summary>
    /// Takes configuration settings from .ini file to setup logging options
    /// </summary>
    public void SetupLogging()
    {
        Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();
        const string LOGGING_SECTION = "logger";

        PatternLayout patternLayout = new PatternLayout();
        patternLayout.ConversionPattern = "%date [%thread] %level %logger - %message%newline";
        patternLayout.ActivateOptions();

        string location = conf.Read("location", LOGGING_SECTION, null);
        string levelName = conf.Read("level", LOGGING_SECTION, "INFO");
        string maxsize = conf.Read("maxsize", LOGGING_SECTION, null);

        Level level = hierarchy.LevelMap[levelName];
        if (level != null)
        {
            hierarchy.Root.Level = level;
        }
        else
        {
            hierarchy.Root.Level = Level.Info;
        }
        
        if (location != null)
        {
            RollingFileAppender roller = new RollingFileAppender();
            roller.AppendToFile = true;
            roller.File = location;
            roller.Layout = patternLayout;
            // FIXME: MaxSizeRollBackups should be INI configurable.
            roller.MaxSizeRollBackups = 5;
            if (maxsize != null)
            {
                roller.MaximumFileSize = maxsize;
            }
            roller.RollingStyle = RollingFileAppender.RollingMode.Size;
            roller.StaticLogFileName = true;
            roller.LockingModel = new FileAppender.MinimalLock();
            roller.ActivateOptions();
            hierarchy.Root.AddAppender(roller);
        } else
        {
            // If 'location' is not specified then log to the console.
            ConsoleAppender console = new ConsoleAppender();
            console.Layout = patternLayout;
            console.ActivateOptions();
            console.Threshold = level;
            hierarchy.Root.AddAppender(console);
        }

        hierarchy.Configured = true;
    }

    /// <summary>
    /// Runs the HTTP Processing for a Primary or Worker Agent
    /// </summary>
    /// <returns>0 if process completes regularly</returns>
    public int Run()
    {
        PaletteHandler handler = new PaletteHandler(this);

        // FIXME: make this configurable in the INI file.
        int reconnectInterval = 10;

        while (true)
        {
            HttpProcessor processor = new HttpProcessor(controllerHost, controllerPort, controllerSsl, controllerTimeoutMilliseconds);
            try
            {
                processor.Connect();

                if (processor.isConnected)
                {
                    processor.Run(handler);
                }
            }
            catch (Exception e)
            {
                /*
                 * HTTP exceptions are explicity caught in HttpProcessor.Run - as are most other exceptions.  If this point is reached,
                 * then there is no other option but log the error and re-establish communication with the contoller.  The most likely
                 * cause of getting here is the socket is closed and a received request can't be sent a response.
                 */
                logger.Error(e.ToString());
            }

            processor.Close();
            logger.Info(String.Format("Connection closed, waiting {0} seconds to reconnect.", reconnectInterval));
            Thread.Sleep(reconnectInterval * 1000);
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
        envPath = conf.Read("path", DEFAULT_SECTION, null);

        // intentionally raise exception if these are not set.
        uuid = conf.Read("uuid", DEFAULT_SECTION);
        installDir = conf.Read("install-dir", DEFAULT_SECTION);
        programDataDir = conf.Read("data-dir", DEFAULT_SECTION, null);
        if (programDataDir == null)
        {
            /*
             * programDataDir == installDir primarily for debugging.
             * For production, data-dir will be specified by the installer.
             */
            programDataDir = installDir;
        }
        
        // allow overriding the hostname in the INI file so that multiple agents may be run on the same system for development.
        if (conf.KeyExists("hostname", DEFAULT_SECTION))
        {
            hostname = conf.Read("hostname", DEFAULT_SECTION);
        }

        controllerHost = conf.Read("host", "controller", DEFAULT_CONTROLLER_HOST);
        controllerPort = conf.ReadInt("port", "controller", DEFAULT_CONTROLLER_PORT);
        controllerSsl = conf.ReadBool("ssl", "controller", false);
        controllerTimeoutMilliseconds = conf.ReadInt("timeout", "controller", DEFAULT_TIMEOUT);

        archivePort = conf.ReadInt("port", "archive", DEFAULT_ARCHIVE_PORT);

        licenseKey = conf.Read("license-key", DEFAULT_SECTION, "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX");
    }

    /// <summary>
    /// Return the first IPv4 address of this system.
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// If the passed in string contains a space, return a quoted versions, otherwise return the original string.
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    private string quote(string s)
    {
        if (s.Contains(" "))
        {
            return '"' + s + '"';
        }
        return s;
    }

    /// <summary>
    /// Turn on the maintenace webserver.
    /// </summary>
    public void startMaintServer(PaletteHandler.ServerControlInfo info)
    {
        string path = StdPath.Combine(programDataDir, "maint", "vars.conf");
        List<string> contents = new List<string>();

        if (info.server_name != null)
        {
            contents.Add("Define SERVER_NAME " + info.server_name);
        }
        else
        {
            contents.Add("Define SERVER_NAME localhost");
        }

        if (info.listen_port > 0)
        {
            contents.Add("Define LISTEN_PORT " + Convert.ToString(info.listen_port));
        }

        if (info.ssl_listen_port > 0)
        {
            contents.Add("Define SSL_LISTEN_PORT " + Convert.ToString(info.ssl_listen_port));
        }

        if (info.ssl_cert_key_file != null)
        {
            contents.Add("Define SSL_CERT_KEY_FILE " + quote(info.ssl_cert_key_file));
        }

        if (info.ssl_cert_file != null)
        {
            contents.Add("Define SSL_CERT_FILE " + quote(info.ssl_cert_file));
        }

        if (info.ssl_cert_chain_file != null)
        {
            contents.Add("Define SSL_CERT_CHAIN_FILE " + quote(info.ssl_cert_chain_file));
        }

        File.WriteAllLines(path, contents.ToArray());

        maintServer.start();
        logger.Info(MAINTENANCE_SERVICE_NAME + " started.");
    }


    /// <summary>
    /// Shutdown the maintenance webserver (if running).
    /// </summary>
    public void stopMaintServer()
    {
        maintServer.stop();
        logger.Info(MAINTENANCE_SERVICE_NAME + " stopped.");
    }

    /// <summary>
    /// Turn on the archive webserver.
    /// </summary>
    public void startArchiveServer()
    {
        string path = StdPath.Combine(programDataDir, "archive", "vars.conf");
        File.WriteAllText(path, "Define LISTEN_PORT " + Convert.ToString(archivePort) + "\r\n");

        archiveServer.start();
        logger.Info(ARCHIVE_SERVICE_NAME + " started on port " + Convert.ToString(archivePort));
    }

    /// <summary>
    /// Shutdown the archive webserver (if running).
    public void stopArchiveServer()
    {
        archiveServer.stop();
        logger.Info(ARCHIVE_SERVICE_NAME + " stopped.");
    }

    //
    public string GetFQDN()
    {
        string domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
        string hostName = Dns.GetHostName();

        if (!hostName.Contains(domainName))            // if the hostname does not already include the domain name
        {
            hostName = hostName + "." + domainName;   // add the domain name part
        }

        return hostName;                              // return the fully qualified domain name
    }
}
