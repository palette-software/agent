using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Collections.Generic;

public class ProcessManager
{
    private string xidDir = "C:\\Palette\\XID";
    private string binDir = "C:\\Program Files\\Tableau\\Tableau Server\\8.1\\bin\\";

    public ProcessManager()
    {
        CheckPaths();
    }

    public ProcessManager(string xidDir, string binDir)
    {
        this.xidDir = xidDir;
        this.binDir = binDir;
        CheckPaths();
    }

    private void CheckPaths()
    {
        if (!Directory.Exists(xidDir))
        {
            throw new FileNotFoundException(xidDir);
        }
        if (!Directory.Exists(binDir))
        {
            throw new FileNotFoundException(binDir);
        }
    }

    public Process Start(int xid, string cmd)
    {
        Process process = new Process();

        // FIXME: check validity of 'cmd' and 'xid'

        string dir = Path.Combine(xidDir, Convert.ToString(xid));
        Directory.CreateDirectory(dir);
        // FIXME: check result

        string stdOutPath = Path.Combine(dir, "stdout");
        string stdErrPath = Path.Combine(dir, "stderr");
        string retPath = Path.Combine(dir, "returncode");
        string tmpPath = Path.Combine(dir, "tmp");

        // Steps:
        //  1. Call the command using cmd.exe.
        //  2. Redirect stderr and stdout to respective file.
        //  3. write the return code to 'tmp'.
        //  4. rename 'tmp' to 'retval' (which should be an atomic FS operation.)
        // Note: A single '&' in cmd.exe is equivalent to ';' in a BASH shell.
        string args = "/c call " + cmd + " 2>" + stdErrPath + " 1>" + stdOutPath;
        args += " & echo %ERRORLEVEL% >" + tmpPath;
        args += " & move " + tmpPath + " " + retPath;

        process.StartInfo.WorkingDirectory = binDir;
        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.Arguments = args;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        WritePid(dir, process.Id);

        return process;
    }

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

    protected void WritePid(string dir, int pid)
    {
        string path = Path.Combine(dir, "pid");
        File.WriteAllText(path, Convert.ToString(pid));
    }

    public bool IsDone(int xid)
    {
        string dir = Path.Combine(xidDir, Convert.ToString(xid));
        if (!Directory.Exists(dir))
        {
            return false;
        }
        string path = Path.Combine(dir, "returncode");
        return File.Exists(path);
    }

    protected int GetInt(int xid, string name)
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

    public int GetPid(int xid)
    {
        return GetInt(xid, "pid");
    }

    public int GetReturnCode(int xid)
    {
        return GetInt(xid, "returncode");
    }

    protected string GetString(int xid, string name)
    {
        string dir = Path.Combine(xidDir, Convert.ToString(xid));
        if (!Directory.Exists(dir))
        {
            return "";
        }
        string path = Path.Combine(dir, name);
        // FIXME: check for file existence and catch IOException(s).
        if (File.Exists(path))
        {
            try
            {
                return File.ReadAllText(path).Trim();
            }
            catch (IOException exc)
            {
                Console.WriteLine(exc.ToString());
            }
        }

        return "";
    }

    public string GetStdOut(int xid)
    {
        return GetString(xid, "stdout");
    }

    public string GetStdErr(int xid)
    {
        return GetString(xid, "stderr");
    }

    public Dictionary<string, object> GetInfo(int xid)
    {
        Dictionary<string, object> d = new Dictionary<string, object>();

        d["xid"] = xid;
        d["run-status"] = "unknown";
        d["pid"] = GetPid(xid);
        
        if (IsDone(xid)) {
            d["run-status"] = "finished";
            d["exit-status"] = GetReturnCode(xid);
            d["stdout"] = GetStdOut(xid);
            d["stderr"] = GetStdErr(xid);
        } else {
            d["run-status"] = "running";
        }

        return d;
    }
}