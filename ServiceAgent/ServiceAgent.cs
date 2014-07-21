using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;

class ServiceAgent : ServiceBase
{
    public const string CONSOLE_EXE = "ConsoleAgent.exe";

    /* state */
    public bool running = false;
    /* The ConsoleAgent process handle */
    public Process process = null;
    /* Watcher thread for the process */
    public Thread thread = null;

    /* Main installation directory */
    string installDir;
    /* Path of the console agent executable */
    string agentPath;
    /* Path of agent upgrade file. */
    string upgradePath;
    /* Configuration file */
    string iniFile;

    public ServiceAgent()
    {
        ServiceName = "Palette Agent";

        installDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        agentPath = Path.Combine(installDir, CONSOLE_EXE);
        string upgradeDir = Path.Combine(installDir, "upgrade");
        upgradePath = Path.Combine(upgradeDir, CONSOLE_EXE);
        string confDir = Path.Combine(installDir, "conf");
        iniFile = Path.Combine(confDir, "agent.ini");
        
        // Configure the level of control available on the service.
        CanStop = true;
        CanPauseAndContinue = false;
        CanHandleSessionChangeEvent = false;

        // Configure the service to log important events to the 
        // Application event log automatically.
        AutoLog = true;
    }

    protected override void OnStart(string[] args)
    {
        EventLog.WriteEntry(this.ServiceName + " starting from '" + installDir + "'");
        running = true;

        //Kick off the Agent as a thread so it doesn't halt the onStart() method
        thread = new Thread(new ThreadStart(this.ThreadRun));
        thread.Start();
    }

    private string appendInfo(string message)
    {
        message += Environment.NewLine;
        message += "InstallDir: " + installDir + Environment.NewLine;
        message += "AgentPath: " + agentPath + Environment.NewLine;
        message += "IniFile: " + iniFile + Environment.NewLine;
        return message;
    }

    private void ThreadRun()
    {
        while (running)
        {
            /* UPGRADE */
            if (File.Exists(upgradePath))
            {
                File.Delete(agentPath);
                File.Move(upgradePath, agentPath);
            }

            /* RUN */
            process = new Process();

            string arguments = "\"" + iniFile + "\"";
            process.StartInfo.FileName = agentPath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.WorkingDirectory = installDir;

            try
            {
                process.Start();
                EventLog.WriteEntry("ConsoleAgent started as PID[" + process.Id.ToString() + "]");
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    string message = "ConsoleAgent ExitCode: " + process.ExitCode.ToString();
                    message = appendInfo(message) + Environment.NewLine;
                    message += stderr + Environment.NewLine;
                    EventLog.WriteEntry(message, EventLogEntryType.Error);
                }
                else
                {
                    EventLog.WriteEntry("ConsoleAgent exited cleanly.");
                }
            }
            catch (Exception e)
            {
                string message = "ConsoleAgent Exception" + Environment.NewLine + e.ToString();
                EventLog.WriteEntry(appendInfo(message), EventLogEntryType.Error);
            }
            process.Close();
            Thread.Sleep(10 * 1000);
        }
    }

    protected override void OnStop()
    {
        EventLog.WriteEntry(this.ServiceName + " stopping...");
        running = false;

        try
        {
            if (process != null)
            {
                process.Kill();
            }
        }
        catch (Exception) { }

        try
        {
            thread.Join();
        }
        catch (Exception) { }
    }

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static void Main()
    {
        ServiceBase.Run(new ServiceAgent());
    }
}
