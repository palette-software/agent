using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

public abstract class AgentProcess
{
    protected const int SLEEP_AMOUNT = 100;
    public int xid;  //controller transaction id    
    protected Process process;
    protected string command;
    protected string commandArgs = null;
    public int windows_process_id;  //actual windows process id
    protected string processStatus;
    protected DateTime processStartTime
    {
        get { return process.StartTime; }
    }
    protected TimeSpan totalProcessorTime
    {
        get { return process.TotalProcessorTime; }
    }
    protected DateTime lastCheckTime;
    protected int runStatus;
    protected bool waitForResults;
    protected bool eventHandled = false;
    public Dictionary<string, object> outgoingBody;

    protected virtual int StartProcess(string processType, bool waitForResults)
    {
        process = new Process();
        windows_process_id = process.Id;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;

        try
        {
            process.Start();

            Console.WriteLine(process.StandardOutput.ReadToEnd());         
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception caught in spawning process!! " + ex.ToString());
            runStatus = -1; 
        }

        if (waitForResults)
        {
            process.Exited += new EventHandler(myProcess_Exited);
            while (!eventHandled)
            {
                process.WaitForExit(SLEEP_AMOUNT);
            }

            runStatus = 1;
        }
        else
        {
            runStatus = 1;
        }

        this.outgoingBody["run-status"] = runStatus;

        return runStatus;        
    }

    // Handle Exited event and display process information. 
    protected void myProcess_Exited(object sender, System.EventArgs e)
    {
        eventHandled = true;
        this.outgoingBody["run-status"] = 2;
        Console.WriteLine("Exit time: {0}\r\n" +
            "Exit code: {1}\r\nElapsed time: {2}", process.ExitTime, process.ExitCode, totalProcessorTime.Seconds);
    }

    public virtual int ReturnStatus() 
    {
        if (process.Responding)
            return 1;
        else
            if (eventHandled)
            {
                return 2;
            }
            else if (runStatus == 3)
            {
                return 3;
            }
            else
            {
                return -1;
            }
    }

    public virtual int KillProcess()
    {
        try
        {
            if (process.Responding)
            {
                process.Kill();
                runStatus = 3;
                this.outgoingBody["run-status"] = runStatus;
            }
        }
        catch (Exception ex) 
        {
            runStatus = -1;
            Console.WriteLine("Error in Killing process " + xid + " " + ex.ToString());
        }
        return runStatus;
    }
}

