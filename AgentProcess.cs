using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Configuration;
using System.Diagnostics;
using System.Threading;

public class ProcessCollection
{
    //Dictionary of xid, process
    public Dictionary<int, AgentProcess> agentProcesses;
    protected static Dictionary<int, int> xidMapping; 

    public ProcessCollection()
    {
        agentProcesses = new Dictionary<int, AgentProcess>();
        xidMapping = new Dictionary<int, int>();
    }

    /// <summary>
    /// Returns the Windows process id for a given controller process id
    /// </summary>
    /// <param name="xid">controller process id</param>
    /// /// <param name="waitForResults">whether or not to wait for results</param>
    /// <returns>Windows process id</returns>
    protected int GetWindowsProcessForXid(int xid)
    {
        if (xidMapping.ContainsKey(xid))
        {
            return xidMapping[xid];
        }
        else
        {
            return -1;
        }
    }

    public int GetProcessStatus(int xid)
    {
        if (agentProcesses.ContainsKey(xid))
        {
            return agentProcesses[xid].ReturnStatus();
        }
        else
        {
            return -1;
        }
    }

    public Dictionary<string, object> GetOutgoingBody(int xid)
    {
        return agentProcesses[xid].outgoingBody;
    }

    //For status, backup and restore calls
    public void AddCLIProcess(int xid, string binaryFolder, string outputFolder, string command, string args)
    {
        if (!agentProcesses.ContainsKey(xid))
        {
            CLIProcess proc = new CLIProcess(xid, binaryFolder, outputFolder, command, args);

            agentProcesses.Add(xid, proc);

            if (!xidMapping.ContainsKey(xid)) xidMapping.Add(xid, proc.windows_process_id);
        }
        else
        {
            throw new Exception("There is already a process with this xid");
        }
    }

    public int KillProcess(int xid)
    {
        if (agentProcesses.ContainsKey(xid))
        {
            AgentProcess jobToKill = agentProcesses[xid];
            return jobToKill.KillProcess();
        }
        else
        {
            return -1;
        }
    }

    public void RemoveProcess(int xid)
    {
        if (agentProcesses.ContainsKey(xid))
        {
            agentProcesses.Remove(xid);
        }
    }

    public void RemoveAllProcesses()
    {
        agentProcesses.Clear();
    }
}

public class CLIProcess : AgentProcess
{
    public string processType = "unknown";
    protected string binFolder;
    protected string outputFolder;
    protected string outputFileName;

    /// <summary>
    /// Process class for cli backup and restore commands
    /// </summary>
    /// <param name="binaryFolder">folder for command executable</param>
    /// <param name="outputFolder">folder for output file</param>
    /// <param name="command">the actual command (i.e. "tabadmin backup")</param>
    /// <param name="args">command args (optional)</param>
    public CLIProcess(int xid, string binaryFolder, string outputFolder, string command, string args)
    {
        this.xid = xid;
        this.binFolder = binaryFolder;  
        this.outputFolder = outputFolder;
        this.command = command;  
        this.commandArgs = args;

        if (this.commandArgs.Contains("status"))
        {
            processType = "status";
            this.outputFileName = "tableau_status " + System.DateTime.Now.ToString("yyyy.mm.dd") + ".out";
        }

        if (this.commandArgs.Contains("backup"))
        {
            processType = "backup";
            this.outputFileName = "tableau_backup " + System.DateTime.Now.ToString("yyyy.mm.dd") + ".out";
        }

        else if (this.commandArgs.Contains("restore"))
        {
            processType = "restore";
            this.outputFileName = "restore " + System.DateTime.Now.ToString("yyyy.mm.dd") + ".out";
        }

        AddOutgoingBody(processType);

        StartProcess(processType, false);
    }

    protected void AddOutgoingBody(string processType)
    {
        this.outgoingBody = new Dictionary<string, object>();

        int unknown = 0;
        this.outgoingBody.Add("run-status", unknown);
        this.outgoingBody.Add("exit-status", unknown);
        this.outgoingBody.Add("xid", this.xid);
        if (processType == "backup" || processType == "restore")
        {
            this.outgoingBody.Add("stdout", "");
            this.outgoingBody.Add("stderr", "");
        }
    }

    protected override int StartProcess(string processType, bool waitForResults)
    {
        Process process = new Process();

        process.StartInfo.WorkingDirectory = this.binFolder;
        process.StartInfo.FileName = this.binFolder + this.command + ".exe";
        process.StartInfo.Arguments = (commandArgs != null) ? (" " + commandArgs + " " + outputFileName) : outputFileName;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        this.waitForResults = waitForResults;

        //process.StartInfo.FileName = @"C:\Program Files\Tableau\Tableau Server\8.1\bin\tabadmin.exe";

        try
        {
            process.Start();

            //this.outgoingBody["stdout"] = process.StandardOutput.ReadToEnd();
            //this.outgoingBody["stderr"] = process.StandardError.ReadToEnd();

            windows_process_id = process.Id;           

            //Console.WriteLine(process.StandardOutput.ReadToEnd());

            using (FileStream fs = new FileStream(outputFolder + outputFileName, FileMode.CreateNew))
            {
                using (BinaryWriter w = new BinaryWriter(fs))
                {
                    //create process thread that puts standard output to file
                    w.Write(process.StandardOutput.ReadToEnd());
                    //process.WaitForExit();  --testing this only
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception caught in spawning process!! " + ex.ToString());
            runStatus = -1;  //Not currently part of spec, but should be?            
        }

        if (waitForResults)
        {
            process.Exited += new EventHandler(myProcess_Exited);
            while (!eventHandled)
            {
                process.WaitForExit(SLEEP_AMOUNT);
            }

            runStatus = 2;
        }
        else
        {
            runStatus = 1;
        }
        this.outgoingBody["run-status"] = runStatus;

        return runStatus;
    }
}

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

