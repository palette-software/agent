using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.ComponentModel;

class prun
{
    static int Main(string[] args)
    {
        string filename = "";  
        string arguments = "";

        //May want to allow for overriding this in the future
        string localOutputFolder = Directory.GetCurrentDirectory();

        if (args.Length < 1)
        {
            //Example: prun tabadmin status -v
            Console.WriteLine("Usage: prun.exe FileToRun [arguments]"); 
            return -1;
        }
        else
        {
            filename = args[0];

            foreach(string arg in args)
            {
                if (arg == args[0]) continue;
                arguments += arg + " "; 
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
    private string stdOutPath = "";
    private string stdErrPath = "";
    private string binDir = "";
    StreamReader standardOutStream;
    StreamReader standardErrStream;
    ProcessStartInfo startInfo;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="filename">the filename of the executable program</param>
    /// <param name="arguments">the corresponding arguments</param>
    /// <param name="localOutputFolder">folder to output stdout.out and stderr.out</param>
    public PRunProcess(string filename, string arguments, string localOutputFolder)
    {       
        //make sure bin for tableau and the agent are in the path
        if (filename == "tabadmin") binDir = "C:\\Program Files\\Tableau\\Tableau Server\\8.1\\bin\\";

        startInfo = new ProcessStartInfo();
        startInfo.WorkingDirectory = binDir;
        startInfo.FileName = Path.Combine(binDir, filename);
        startInfo.Arguments = arguments;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        //TODO: Use Environment variable property of StartInfo
        //startInfo.EnvironmentVariables.Add();

        stdOutPath = Path.Combine(localOutputFolder, "stdout");
        stdErrPath = Path.Combine(localOutputFolder, "stderr");        
    }

    /// <summary>
    /// Runs the process and writes StdOut and StdErr to files.  Spawns threads to handle 
    /// StdOut and StdErr output
    /// </summary>
    /// <returns>Error code (any value but 0 indicates an error)</returns>
    [System.Obsolete("use RunWithEventOutput()")]
    public int RunWithThreadedOutput()
    {
        Thread standardOutputThread = null;
        Thread standardErrorThread = null;

        int exitCode = -1;

        try
        {
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();

                standardOutStream = process.StandardOutput;
                standardOutputThread = StartThread(new ThreadStart(WriteStandardOutput), "StandardOutput");

                standardErrStream = process.StandardError;
                standardErrorThread = StartThread(new ThreadStart(WriteStandardError), "StandardError");

                process.WaitForExit();
                exitCode = process.ExitCode;
            }
        }

        finally  // Ensure that the threads do not persist beyond the process being run
        {
            if (standardOutputThread != null) standardOutputThread.Join();
            if (standardErrorThread != null) standardErrorThread.Join();

            RenameFiles();
        }

        return exitCode;
    }

    private static Thread StartThread(ThreadStart startInfo, string name)
    {
        Thread thread = new Thread(startInfo);
        thread.IsBackground = true;
        thread.Name = name;
        thread.Start();
        return thread;
    }

    private void WriteStandardOutput()
    {
        FileStream fs = File.Open(stdOutPath + ".tmp", FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        using (StreamWriter writer = new StreamWriter(fs))
        using (StreamReader reader = standardOutStream)
        {
            writer.AutoFlush = true;
            for ( ; ; )
            {
                string textLine = reader.ReadLine();

                if (textLine == null) break;

                writer.WriteLine(textLine);
            }
        }
    }

    private void WriteStandardError()
    {
        FileStream fs = File.Open(stdErrPath + ".tmp", FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        using (StreamWriter writer = new StreamWriter(fs))
        using (StreamReader reader = standardErrStream)
        {
            writer.AutoFlush = true;
            for ( ; ; )
            {
                string textLine = reader.ReadLine();

                if (textLine == null) break;

                writer.WriteLine(textLine);
            }
        }
    }

    private void RenameFiles()
    {
        File.Move(stdOutPath + ".tmp", stdOutPath + ".out");
        File.Move(stdErrPath + ".tmp", stdErrPath + ".out");
    }

    /// <summary>
    /// Alternative approach using event driven output to files
    /// WARNING: Times out after 10 minutes!!
    /// </summary>
    /// <returns>Error code (any value but 0 indicates an error)</returns>
    public int RunWithEventOutput()
    {
        int timeout = 600000;  //TODO: Remove or increase this?
        int exitCode = -1;

        using (Process process = new Process())
        {
            process.StartInfo = startInfo;

            FileStream fsOut = File.Open(stdOutPath + ".tmp", FileMode.Create, FileAccess.Write, FileShare.Read);
            FileStream fsErr = File.Open(stdErrPath + ".tmp", FileMode.Create, FileAccess.Write, FileShare.Read);

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

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (process.WaitForExit(timeout) &&
                    outputWaitHandle.WaitOne(timeout) &&
                    errorWaitHandle.WaitOne(timeout))
                {
                    // Process completed. Check process.ExitCode here. 
                    exitCode = process.ExitCode;
                    Console.WriteLine("Process completed");
                }
                else
                {
                    // Process timed out
                    Console.WriteLine("Error: Process timed out");
                    exitCode = -1;                    
                    return exitCode;
                }                
            }
        }
        RenameFiles();
        return exitCode;
    }
}
