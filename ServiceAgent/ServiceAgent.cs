using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using System.ServiceProcess;
using System.Text;
using System.Threading;


namespace ServiceAgent
{
    public partial class ServiceAgent : ServiceBase
    {
        //private System.Timers.Timer timer;
        //private Thread newThread;

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
            // Obtain the interval between log entry writes from the first argument. Use 60000 milliseconds by
            // default and enforce a 60000 millisecond minimum.
            //double interval;

            //try
            //{
            //    interval = Double.Parse(args[0]);
            //    interval = Math.Max(60000, interval);
            //}
            //catch
            //{
            //    interval = 60000;
            //}

            EventLog.WriteEntry("ServiceAgent Service starting...");
            //EventLog.WriteEntry(String.Format("ServiceAgent Service starting. " +
            //    "Writing log entries every {0} milliseconds...", interval));            

            //timer = new System.Timers.Timer();
            //timer.Interval = interval;
            //timer.AutoReset = true;
            //timer.Elapsed += new ElapsedEventHandler(WriteLogEntry);
            //timer.Start();            

            //Kick off the Agent as a thread so it doesn't halt the onStart() method
            Thread t = new Thread(new ThreadStart(this.InitAgent));
            t.Start();
        }

        private void InitAgent()
        {
            // FIXME:
            //Agent.RunAgent();
        }

        protected override void OnStop()
        {
            EventLog.WriteEntry("ServiceAgent Service stopping...");
            //newThread.Abort();
            //timer.Stop();

            //timer.Dispose();
            //timer = null;
        }
    }
}
