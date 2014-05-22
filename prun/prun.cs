using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.ComponentModel;
using System.Reflection;

class prun
{
    static int Main(string[] args)
    {
        string filename = "";  
        string arguments = "";

        //System.Diagnostics.Debugger.Launch();

        //May want to allow for overriding this in the future
        string localOutputFolder = Directory.GetCurrentDirectory();

        if (args.Length < 1)
        {
            //Example: prun tabadmin status -v
            Console.Error.WriteLine("Usage: prun.exe FileToRun [arguments]"); 
            return -1;
        }
        else
        {
            filename = args[0];

            foreach(string arg in args)
            {
                if (arg == args[0]) continue;
                if (arg.Contains(' '))
                {
                    // quote any argument containing a space.
                    arguments += "\"" + arg + "\" ";
                }
                else
                {
                    arguments += arg + " ";
                }
            }

            arguments = arguments.Trim();
        }

        PRunProcess proc = new PRunProcess(filename, arguments, localOutputFolder);

        return proc.RunWithEventOutput();        
    }
}

/// <summary>
/// Encapsulates a process and writes StdOut and StdErr to two files in the current 
/// working directory
/// </summary>
public class PRunProcess
{
    private string stdOutPath;
    private string stdErrPath;
    private string returnCdTmpPath;
    private string returnCdPath;
    ProcessStartInfo startInfo;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="filename">the filename of the executable program</param>
    /// <param name="arguments">the corresponding arguments</param>
    /// <param name="localOutputFolder">folder to output stdout.out and stderr.out</param>
    public PRunProcess(string filename, string arguments, string localOutputFolder)
    {
        startInfo = new ProcessStartInfo();
        startInfo.FileName = filename;
        startInfo.Arguments = arguments;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        // Windows doesn't explicitly use the PWD environment variable, so it can be used to pass the working directory.
        string pwd = Environment.GetEnvironmentVariable("PWD");
        if (pwd != null)
        {
            startInfo.WorkingDirectory = pwd;
        }

        // Environment variables are inherited from the agent.
        // The XID directory is the working directory from the agent call.

        stdOutPath = Path.Combine(localOutputFolder, "stdout");
        stdErrPath = Path.Combine(localOutputFolder, "stderr");
        returnCdPath = Path.Combine(localOutputFolder, "returncode");
        returnCdTmpPath = Path.Combine(localOutputFolder, "tmp"); 
    }

    /// <summary>
    /// Writes return value of the process to a file called tmp in the current directory
    /// After write completes, renames tmp to returncode
    /// </summary>
    private void WriteReturnCode(int returnCode)
    {
        File.WriteAllText(returnCdTmpPath, Convert.ToString(returnCode));

        File.Move(returnCdTmpPath, returnCdPath);
    }

    /// <summary>
    /// Alternative approach using event driven output to files
    /// </summary>
    /// <returns>Error code (any value but 0 indicates an error)</returns>
    public int RunWithEventOutput()
    {
        int exitCode = -1;

        using (Process process = new Process())
        {
            process.StartInfo = startInfo;

            FileStream fsOut = File.Open(stdOutPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            FileStream fsErr = File.Open(stdErrPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            using (StreamWriter outputWriter = new StreamWriter(fsOut))
            using (StreamWriter errorWriter = new StreamWriter(fsErr))
            {
                outputWriter.AutoFlush = true;
                errorWriter.AutoFlush = true;

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        outputWaitHandle.Set();
                    }
                    else
                    {
                        outputWriter.WriteLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        errorWaitHandle.Set();
                    }
                    else
                    {
                        errorWriter.WriteLine(e.Data);
                    }
                };

                exitCode = 0;
                try
                {
                    process.Start();
                }
                catch (InvalidOperationException e)
                {
                    Console.Error.WriteLine("InvalidOperationException" +
                      e.ToString());
                    exitCode = -1;
                }
                catch (Win32Exception e)
                {
                    Console.Error.WriteLine("ObjectDisposedException" + 
                      e.ToString());
                    exitCode = -1;
                }

                if (exitCode == 0)
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();
                    outputWaitHandle.WaitOne();
                    errorWaitHandle.WaitOne();

                    exitCode = process.ExitCode;
                }
            }
        }

        try
        {
            WriteReturnCode(exitCode);
        }
        catch (IOException e)
        {
            Console.Error.WriteLine(e.ToString());
        }

        if (File.Exists(returnCdTmpPath))
        {
            File.Delete(returnCdTmpPath);
        }

        return exitCode;
    }
}
