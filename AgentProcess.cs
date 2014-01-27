using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Configuration;
using System.Diagnostics;

public class ProcessCollection
{
    //Dictionary of xid, process
    public Dictionary<int, AgentProcess> agentProcesses;

    public ProcessCollection()
    {
        agentProcesses = new Dictionary<int, AgentProcess>();
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

    public void AddCLIProcess(int xid, string binaryFolder, string outputFolder, string command, string args)
    {
        if (!agentProcesses.ContainsKey(xid))
        {
            CLIProcess proc = new CLIProcess(binaryFolder, outputFolder, command, args);

            agentProcesses.Add(xid, proc);
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

}

public class CLIProcess : AgentProcess
{
    public CLIProcess(int xid) : base(xid) 
    { 
    }

    /// <summary>
    /// Process class for requests of type /cli
    /// </summary>
    /// <param name="binaryFolder">folder for command executable</param>
    /// <param name="outputFolder">folder for output file</param>
    /// <param name="command">the actual command (i.e. "tabadmin backup")</param>
    /// <param name="args">command args (optional)</param>
    public CLIProcess(string binaryFolder, string outputFolder, string command, string args)
    {
        this.binFolder = binFolder;  
        this.outputFolder = outputFolder;
        this.command = command;  
        this.commandArgs = args;

        if (this.command == "tabadmin backup")
        {
            this.outputFileName = "tableau_backup " + System.DateTime.Now.ToString("yyyy.mm.dd") + ".out";
        }
        else if (this.command == "tabadmin restore")
        {
            this.outputFileName = "restore " + System.DateTime.Now.ToString("yyyy.mm.dd") + ".out";
        }
    }
}

public abstract class AgentProcess
{
    const int SLEEP_AMOUNT = 100;
    public int xid;  //controller transaction id
    protected static Dictionary <int, int> xidMapping; 
    protected Process process;
    protected string command;
    protected string commandArgs = null;
    protected int windows_process_id;  //actual windows process id
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
    int runStatus;
    bool waitForResults;
    protected string binFolder;
    protected string outputFolder;
    protected string outputFileName;
    bool eventHandled = false;

    public AgentProcess()
    {
    }

    public AgentProcess(int xid)
    {
        this.xid = xid;
        if (xidMapping == null) xidMapping = new Dictionary<int, int>();
    }

    /// <summary>
    /// Returns the Windows process id for a given controller process id
    /// </summary>
    /// <param name="xid">controller process id</param>
    /// /// <param name="waitForResults">whether or not to wait for results</param>
    /// <returns>Windows process id</returns>
    protected int GetWindowsProcessForXid(int xid, bool waitForResults)
    {
        this.waitForResults = waitForResults;
        if (xidMapping.ContainsKey(xid))
        {
            return xidMapping[xid];
        }
        else
        {
            return -1;
        }
    }

    protected virtual int StartProcess()
    {
        process = new Process();
        windows_process_id = process.Id;
        process.StartInfo.FileName = binFolder + command;
        process.StartInfo.Arguments = (commandArgs != null)? (" " + commandArgs + " " + outputFileName): outputFileName;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;

        try
        {
            process.Start();

            if (!xidMapping.ContainsKey(xid)) xidMapping.Add(xid, windows_process_id);

            Console.WriteLine(process.StandardOutput.ReadToEnd());

            using (FileStream fs = new FileStream(outputFileName, FileMode.CreateNew))
            {
                using (BinaryWriter w = new BinaryWriter(fs))
                {
                    w.Write(process.StandardOutput.ReadToEnd());
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

            runStatus = 1;
        }
        else
        {
            runStatus = 1;
        }

        return runStatus;        
    }

    // Handle Exited event and display process information. 
    private void myProcess_Exited(object sender, System.EventArgs e)
    {
        eventHandled = true;
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

