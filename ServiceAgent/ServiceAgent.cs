using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;

class ServiceAgent : ServiceBase
{
    string installDir;
    string iniFile;

    public ServiceAgent()
    {
        ServiceName = "Palette Agent";

        installDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
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
        EventLog.WriteEntry(this.ServiceName + " starting...");

        //Kick off the Agent as a thread so it doesn't halt the onStart() method
        Thread t = new Thread(new ThreadStart(this.ThreadRun));
        t.Start();
    }

    private void ThreadRun()
    {
        Agent agent = new Agent(iniFile);
        agent.Run();
    }

    protected override void OnStop()
    {
        EventLog.WriteEntry(this.ServiceName + " stopping...");
    }

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static void Main()
    {
        ServiceBase.Run(new ServiceAgent());
    }
}
