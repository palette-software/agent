﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using log4net;
using log4net.Config;

/// <summary>
/// Manages all agent processes
/// </summary>
public class ProcessManager
{
    private string xidDir; 
    private string binDir;
    private string envPath;

    //This has to be put in each class for logging purposes
    private static readonly log4net.ILog logger = log4net.LogManager.GetLogger
    (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="xidDir">Folder named by xid</param>
    /// <param name="binDir">Folder containing command executable</param>
    public ProcessManager(string xidDir, string binDir, string envPath)
    {
        this.xidDir = xidDir;
        this.binDir = binDir;
        this.envPath = envPath;

        string currentPath = Environment.GetEnvironmentVariable("Path");

        if (envPath != null && envPath.Length > 0)
        {
            string path = envPath + Path.PathSeparator.ToString() + currentPath;
            Environment.SetEnvironmentVariable("Path", path);
        }

        if (!Directory.Exists(xidDir))
        {
            Directory.CreateDirectory(xidDir);
        }
    }

    /// <summary>
    /// Starts an agent process
    /// </summary>
    /// <param name="xid">agent process id</param>
    /// <param name="cmd">command string</param>
    /// <returns>a agent process</returns>
    public Process Start(UInt64 xid, string cmd, Dictionary<string, string> env, bool immediate)
    {
        Process process = new Process();

        // FIXME: check validity of 'cmd' and 'xid'
        string dir = StdPath.Combine(xidDir, Convert.ToString(xid));
        Directory.CreateDirectory(dir);
        // FIXME: check result

        process.StartInfo.WorkingDirectory = dir;
        process.StartInfo.FileName = StdPath.Combine(binDir, "prun.exe");
        process.StartInfo.Arguments = cmd;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        if (env != null)
        {
            foreach (KeyValuePair<string, string> entry in env)
            {
                if (process.StartInfo.EnvironmentVariables.ContainsKey(entry.Key))
                {
                    process.StartInfo.EnvironmentVariables.Remove(entry.Key);
                }
                process.StartInfo.EnvironmentVariables.Add(entry.Key, entry.Value);
            }
        }


        try
        {
            process.Start();
            WritePid(dir, process.Id);
            WriteCmd(dir, cmd);
        }
        catch(Exception exc)
        {
            logger.Error("Error launching process: " + exc.Message);
        }

        if (immediate)
        {
            process.WaitForExit();
        }

        return process;
    }

    /// <summary>
    /// Removes the folder and file containing StdOut and StdErr for a given process id.  
    /// This is done after response is sent to controller verifying process completion
    /// </summary>
    /// <param name="xid">agent process id</param>
    public void Cleanup(UInt64 xid)
    {
        // FIXME: check xid
        string dir = StdPath.Combine(xidDir, Convert.ToString(xid));
        if (!Directory.Exists(dir))
        {
            return;
        }
        DirectoryInfo dirInfo = new DirectoryInfo(dir);
        dirInfo.Delete(true);
    }

    /// <summary>
    /// Writes the Windows process id to file
    /// </summary>
    /// <param name="dir">Folder</param>
    /// <param name="pid">Windows process id</param>
    private void WritePid(string dir, int pid)
    {
        string path = StdPath.Combine(dir, "pid");
        File.WriteAllText(path, Convert.ToString(pid));     
    }

    /// <summary>
    /// Writes the Windows process id to file
    /// </summary>
    /// <param name="dir">Folder</param>
    /// <param name="pid">Windows process id</param>
    private void WriteCmd(string dir, string cmd)
    {
        string path = StdPath.Combine(dir, "cmd");
        File.WriteAllText(path, cmd);
    }

    /// <summary>
    /// Checks for existence of xid file which indicates process is complete
    /// </summary>
    /// <param name="xid">Agent process id</param>
    /// <returns>true if complete, false otherwise</returns>
    private bool IsDone(UInt64 xid)
    {
        string dir = StdPath.Combine(xidDir, Convert.ToString(xid));
        if (!Directory.Exists(dir))
        {
            return false;
        }
        string path = StdPath.Combine(dir, "returncode");
        return File.Exists(path);
    }

    /// <summary>
    /// Returns an Int32 status code for specific process id  
    /// </summary>
    /// <param name="xid">Agent process id</param>
    /// <param name="name">File name</param>
    /// <returns>Status code</returns>
    private int GetInt(UInt64 xid, string name)
    {
        string dir = StdPath.Combine(xidDir, Convert.ToString(xid));
        if (!Directory.Exists(dir))
        {
            return -1;
        }
        string path = StdPath.Combine(dir, name);
        string val = File.ReadAllText(path).Trim();
        return Convert.ToInt32(val);
    }

    /// <summary>
    /// Gets Windows process id for specified agent process id
    /// </summary>
    /// <param name="xid">Agent process id</param>
    /// <returns></returns>
    private int GetPid(UInt64 xid)
    {
        return GetInt(xid, "pid");
    }

    /// <summary>
    /// Gets return code (int) for a given agent process id
    /// </summary>
    /// <param name="xid">Agent process id</param>
    /// <returns>return code</returns>
    private int GetReturnCode(UInt64 xid)
    {
        return GetInt(xid, "returncode");
    }

    /// <summary>
    /// Returns the text from output file for specified agent process id
    /// </summary>
    /// <param name="xid">Agent process id</param>
    /// <param name="name">File name</param>
    /// <returns>Output text</returns>
    private string GetString(UInt64 xid, string name)
    {
        string dir = StdPath.Combine(xidDir, Convert.ToString(xid));
        if (!Directory.Exists(dir))
        {
            return "";
        }
        string path = StdPath.Combine(dir, name);

        if (File.Exists(path))
        {
            try
            {
                return File.ReadAllText(path).Trim();
            }
            catch (IOException exc)
            {
                logger.Error(exc.ToString());
            }
        }

        return "";
    }

    /// <summary>
    /// Gets the StdOut for a given agent process id
    /// </summary>
    /// <param name="xid">Agent process id</param>
    /// <returns>StdOut string</returns>
    private string GetStdOut(UInt64 xid)
    {
        return GetString(xid, "stdout");
    }

    /// <summary>
    /// Gets the StdErr for a given agent process id
    /// </summary>
    /// <param name="xid">Agent process id</param>
    /// <returns>StdErr string</returns>
    private string GetStdErr(UInt64 xid)
    {
        return GetString(xid, "stderr");
    }

    /// <summary>
    /// Populates dictionary for a given agent process id
    /// </summary>
    /// <param name="xid">Agent process id</param>
    /// <returns>A dictionary to be encoded into JSON</returns>
    public Dictionary<string, object> GetInfo(UInt64 xid)
    {
        Dictionary<string, object> d = new Dictionary<string, object>();

        d["xid"] = xid;
        d["run-status"] = "unknown";
        d["pid"] = GetPid(xid);
        
        if (IsDone(xid)) {
            d["run-status"] = "finished";
            d["exit-status"] = GetReturnCode(xid);
            d["stdout"] = GetStdOut(xid).Replace("\r", "");
            d["stderr"] = GetStdErr(xid).Replace("\r", "");
        } else {
            d["run-status"] = "running";
        }

        return d;
    }
}