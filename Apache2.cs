using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.IO;
using System.Threading;

class Apache2
{
    protected string name;
    protected string conf;
    protected string topDir;
    protected string srvRootDir;
    protected string logDir;
    protected string baseArgs;
    protected string fileName;

    public Apache2(string name, string conf, string topDir)
    {
        this.name = name;
        this.conf = conf;
        this.topDir = topDir;
        this.srvRootDir = Path.Combine(topDir, "apache2");
        this.logDir = Path.Combine(topDir, "logs");
        string binDir = Path.Combine(srvRootDir, "bin");
        fileName = Path.Combine(binDir, "httpd.exe");
        string startupLog = Path.Combine(logDir, "startup.log");
        baseArgs = "-f \"" + conf + "\" -n \"" + name + "\" -E \"" + startupLog;
    }

    protected void run(string args)
    {
        Process process = new Process();

        process.StartInfo.WorkingDirectory = srvRootDir;
        process.StartInfo.FileName = fileName;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.Arguments = args;

        process.Start();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception(error);
        }   
    }

    protected bool IsInstalled()
    {
        ServiceController sc = new ServiceController(name);
        bool val = false;
        try
        {
            // access throws InvalidOperationException if not installed.
            string displayName = sc.DisplayName;
            val = true;
        }
        catch (InvalidOperationException) { }
        finally
        {
            sc.Close();
        }
        return val;
    }

    protected void DoInstall()
    {
        string args = "-k install " + baseArgs;
        run(args);
    }

    protected void DoStart()
    {
        string args = "-k start " + baseArgs;
        run(args);
    }

    protected void DoStop()
    {
        string args = "-k stop " + baseArgs;
        run(args);
    }

    protected void DoUninstall()
    {
        string args = "-k uninstall " + baseArgs;
        run(args);
    }

    public void start()
    {
        DoInstall();
        try
        {
            DoStart();
        }
        catch (Exception e)
        {
            DoUninstall();
            throw e;
        }
    }

    public void stop()
    {
        if (!IsInstalled())
        {
            // If the server isn't running, i.e. installed, just no-op to avoid the 500 error due to the failed httpd call.
            return;
        }

        // explicitly stop but still continue on errors.
        try
        {
            DoStop();
        }
        catch (Exception){}

        // uninstall implicitly stops if necessary.
        DoUninstall();
    }
}
