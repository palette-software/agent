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
    protected string outputFolder;  //The base output folder
    protected string fullOutputPath;  //The full path for a CLI output folder (i.e. C:\Temp\XID\23\returncode)
    protected string outputFileName;

    /// <summary>
    /// Process class for cli backup and restore commands
    /// </summary>
    /// /// <param name="xid">process id from controller</param>
    /// <param name="binaryFolder">folder for command executable</param>
    /// <param name="outputFolder">folder for output file</param>
    /// <param name="command">the actual command (i.e. "tabadmin backup")</param>
    /// <param name="args">command args (optional)</param>
    public CLIProcess(int xid, string binaryFolder, string outputFolder, string command, string args)
    {
        this.xid = xid;
        this.binFolder = binaryFolder;
        this.outputFolder = outputFolder;
        this.fullOutputPath = outputFolder + "\\XID\\" + xid.ToString() + "\\";

        this.command = command;
        this.commandArgs = args;

        try
        {
            if (!Directory.Exists(fullOutputPath)) Directory.CreateDirectory(fullOutputPath);
        }
        catch (Exception exc)
        {
            Console.WriteLine("Error creating folder: " + exc.ToString());
        }

        //TODO: Change the output file names
        if (this.commandArgs.Contains("status"))
        {
            processType = "status";
        }

        else if (this.commandArgs.Contains("backup"))
        {
            processType = "backup";
        }

        else if (this.commandArgs.Contains("restore"))
        {
            processType = "restore";
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

        ThreadProcess tp = new ThreadProcess(process, fullOutputPath);

        ThreadStart threadDelegate = new ThreadStart(tp.SpawnThreadProcess);

        Thread newThread = new Thread(threadDelegate);

        newThread.Start();

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

    protected void Test()
    {
        runStatus = 1;
    }
}