using System;
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
    public Process Start(int xid, string cmd)
    {
        Process process = new Process();

        // FIXME: check validity of 'cmd' and 'xid'

        string dir = Path.Combine(xidDir, Convert.ToString(xid));
        Directory.CreateDirectory(dir);
        // FIXME: check result

        //string stdOutPath = Path.Combine(dir, "stdout");
        //string stdErrPath = Path.Combine(dir, "stderr");
        //string retPath = Path.Combine(dir, "returncode");
        //string tmpPath = Path.Combine(dir, "tmp");

        // Steps:
        //  1. Call the command using cmd.exe.
        //  2. Redirect stderr and stdout to respective file.
        //  3. write the return code to 'tmp'.
        //  4. rename 'tmp' to 'retval' (which should be an atomic FS operation.)
        // Note: A single '&' in cmd.exe is equivalent to ';' in a BASH shell.
        //string args = "/c call " + cmd + " 2>" + stdErrPath + " 1>" + stdOutPath;
        //args += " & echo %ERRORLEVEL% >" + tmpPath;
        //args += " & move " + tmpPath + " " + retPath;

        //process.StartInfo.WorkingDirectory = binDir;
        process.StartInfo.WorkingDirectory = dir;
        process.StartInfo.FileName = binDir + "\\prun.exe";  //For some reason Path.Combine fails here
        process.StartInfo.Arguments = cmd;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        try
        {
            process.Start();
            WritePid(dir, process.Id);
        }
        catch(Exception exc)
        {
            logger.Error("Error launching process: " + exc.Message);
        }

        return process;
    }

    /// <summary>
    /// Removes the folder and file containing StdOut and StdErr for a given process id.  
    /// This is done after response is sent to controller verifying process completion
    /// </summary>
    /// <param name="xid">agent process id</param>
    public void Cleanup(int xid)
    {
        // FIXME: check xid
        string dir = Path.Combine(xidDir, Convert.ToString(xid));
        if (!Directory.Exists(dir))
        {
            return;
        }
        DirectoryInfo dirInfo = new DirectoryInfo(dir);
        dirInfo.Delete(true);
    }

    /// <summary>
    /// Writes windows process id to file
    /// </summary>
    /// <param name="dir">Folder</param>
    /// <param name="pid">Windows process id</param>
    private void WritePid(string dir, int pid)
    {
        string path = Path.Combine(dir, "pid");

        do
        {
            File.WriteAllText(path, Convert.ToString(pid));

        } while ((System.IO.File.Exists(path) != true));        
    }

    /// <summary>
    /// Checks for existence of xid file which indicates process is complete
    /// </summary>
    /// <param name="xid">Agent process id</param>
    /// <returns>true if complete, false otherwise</returns>
    private bool IsDone(int xid)
    {
        string dir = Path.Combine(xidDir, Convert.ToString(xid));
        if (!Directory.Exists(dir))
        {
            return false;
        }
        string path = Path.Combine(dir, "returncode");
        return File.Exists(path);
    }

    /// <summary>
    /// Returns an Int32 status code for specific process id  
    /// </summary>
    /// <param name="xid">Agent process id</param>
    /// <param name="name">File name</param>
    /// <returns>Status code</returns>
    private int GetInt(int xid, string name)
    {
        string dir = Path.Combine(xidDir, Convert.ToString(xid));
        if (!Directory.Exists(dir))
        {
            return -1;
        }
        string path = Path.Combine(dir, name);
        string val = File.ReadAllText(path).Trim();
        return Convert.ToInt32(val);
    }

    /// <summary>
    /// Gets Windows process id for specified agent process id
    /// </summary>
    /// <param name="xid">Agent process id</param>
    /// <returns></returns>
    private int GetPid(int xid)
    {
        return GetInt(xid, "pid");
    }

    /// <summary>
    /// Gets return code (int) for a given agent process id
    /// </summary>
    /// <param name="xid">Agent process id</param>
    /// <returns>return code</returns>
    private int GetReturnCode(int xid)
    {
        return GetInt(xid, "returncode");
    }

    /// <summary>
    /// Returns the text from output file for specified agent process id
    /// </summary>
    /// <param name="xid">Agent process id</param>
    /// <param name="name">File name</param>
    /// <returns>Output text</returns>
    private string GetString(int xid, string name)
    {
        string dir = Path.Combine(xidDir, Convert.ToString(xid));
        if (!Directory.Exists(dir))
        {
            return "";
        }
        string path = Path.Combine(dir, name);

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
    private string GetStdOut(int xid)
    {
        return GetString(xid, "stdout");
    }

    /// <summary>
    /// Gets the StdErr for a given agent process id
    /// </summary>
    /// <param name="xid">Agent process id</param>
    /// <returns>StdErr string</returns>
    private string GetStdErr(int xid)
    {
        return GetString(xid, "stderr");
    }

    /// <summary>
    /// Populates dictionary for a given agent process id
    /// </summary>
    /// <param name="xid">Agent process id</param>
    /// <returns>A dictionary to be encoded into JSON</returns>
    public Dictionary<string, object> GetInfo(int xid)
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