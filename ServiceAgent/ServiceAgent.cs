using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Reflection;


namespace ServiceAgent
{
    public partial class ServiceAgent : ServiceBase
    {
        public ServiceAgent()
        {
            ServiceName = "Service Agent";

            InitializeComponent();

            // Configure the level of control available on the service.
            CanStop = true;
            CanPauseAndContinue = false;
            CanHandleSessionChangeEvent = false;

            // Configure the service to log important events to the 
            // Application event log automatically.
            AutoLog = true;

            if (!System.Diagnostics.EventLog.SourceExists("ServiceAgentSource"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                   "ServiceAgentSource", "ServiceAgentLog");
            }
            eventLog1.Source = "ServiceAgentSource";
            eventLog1.Log = "ServiceAgentLog";
        }

        // The method executed when the timer expires and writes an entry to the Application event log.
        private void WriteLogEntry(object sender, ElapsedEventArgs e)
        {
            // Use the EventLog object automatically configured by the ServiceBase class to write to the event log. 
            EventLog.WriteEntry("ServiceAgent Service active : " + e.SignalTime);
        }

        protected override void OnStart(string[] args)
        {
            EventLog.WriteEntry("ServiceAgent Service starting...");   

            //Kick off the Agent as a thread so it doesn't halt the onStart() method
            Thread t = new Thread(new ThreadStart(this.InitAgent));
            t.Start();
        }

        private void InitAgent()
        {
            string inifile = "";

            string ver = Agent.GetTableauVersion();

            if (ver == null)
            {
                inifile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\other.ini";
            }
            else
            {
                inifile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\primary.ini";
            }   

            Agent agent = new Agent(inifile);
            agent.Run();
        }

        protected override void OnStop()
        {
            EventLog.WriteEntry("ServiceAgent Service stopping...");
        }
    }
}
