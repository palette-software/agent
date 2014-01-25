using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Configuration;
using System.Diagnostics;

public class CLIProcess : AgentProcess
{   
    public CLIProcess(string binaryFolder, string outputFolder, string command, string args)
    {
        this.binFolder = binFolder;
        this.outputFolder = outputFolder;
        this.command = command;  //i.e. "tabadmin backup"  
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
    protected int xid;  //controller transaction id
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
    protected enum CommandType
    {
        status,
        backup,
        restore,
        copy
    }
    protected string binFolder;
    protected string outputFolder;
    protected string outputFileName;

    protected virtual int StartProcess()
    {
        process = new Process();
        windows_process_id = process.Id;
        process.StartInfo.FileName = binFolder + command;
        process.StartInfo.Arguments = (commandArgs != null)? (" " + commandArgs + " " + outputFileName): outputFileName;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.Start();

        Console.WriteLine(process.StandardOutput.ReadToEnd());

        using (FileStream fs = new FileStream(outputFileName, FileMode.CreateNew))
        {
            using (BinaryWriter w = new BinaryWriter(fs))
            {
                w.Write(process.StandardOutput.ReadToEnd());
            }
        }

        process.WaitForExit();

        return 0;
    }

    public virtual string ReturnStatus() //TODO: use try/catch, Make more comprehensive
    {
        if (process.Responding)
            return "Running for " + totalProcessorTime.Minutes + " minutes";
        else
            if (process.HasExited)
            {
                return "Process has exited";
            }
            else
            {
                return "Not Responding";
            }
    }

    public virtual void KillProcess()
    {
        try
        {
            process.Kill();
        }
        catch (Exception ex) 
        {
            //TODO: Need to handle this smoothly
            Console.WriteLine(ex.ToString());
        }
        
    }
}

