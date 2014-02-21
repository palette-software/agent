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
        string localOutputFolder = "";

        if (args.Length < 1)
        {
            //Example: prun tabadmin status -v
            Console.WriteLine("Usage: prun.exe FileToRun [arguments]");
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

            if (localOutputFolder == "") localOutputFolder = Directory.GetCurrentDirectory();
        }

        PRunProcess proc = new PRunProcess(filename, arguments, localOutputFolder);

        return proc.Run();        
    }
}

/// <summary>
/// Encapsulates a process and writes StdOut and StdErr to two files in the current 
/// working directory
/// </summary>
public class PRunProcess
{
    static string stdOutPath = "";
    static string stdErrPath = "";
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
        if (filename == "tabadmin") binDir = "C:\\Program Files\\Tableau\\Tableau Server\\8.1\\bin\\";

        startInfo = new ProcessStartInfo();
        startInfo.WorkingDirectory = binDir;
        startInfo.FileName = Path.Combine(binDir, filename);
        startInfo.Arguments = arguments;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        stdOutPath = Path.Combine(localOutputFolder, "stdout");
        stdErrPath = Path.Combine(localOutputFolder, "stderr");

        
    }

    /// <summary>
    /// Runs the process and writes StdOut and StdErr to files
    /// </summary>
    /// <returns>Error code (0 for no errors)</returns>
    public int Run()
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

            File.Move(stdOutPath + ".tmp", stdOutPath + ".out");
            File.Move(stdErrPath + ".tmp", stdErrPath + ".out");
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
}


/// <summary>
/// Alternative approach, much simpler but writes StdOut and StdErr to StringBuilder objects and 
/// then all at once to files once the process is finished
/// </summary>
/*
private class prun_alt
{
    static int Main(string[] args)
    {
        int timeout = 60000;
        int exitCode = -1;
        string filename = "";
        string arguments = "";
        string localOutputFolder = "";
        string processId = "?";

        if (args.Length < 1)
        {
            Console.WriteLine(@"Usage: prun.exe FileToRun [/v arguments] [/o LocalOutputFolder] [/p ProcessID]");
        }
        else
        {
            filename = args[0];

            int i = 0;
            foreach (string arg in args)
            {
                if (arg == @"/v") arguments = args[i + 1];  //FIXME: include multiple args
                if (arg == @"/o") localOutputFolder = args[i + 1];
                if (arg == @"/p") processId = args[i + 1].ToString().TrimEnd();
                i++;
            }

            if (localOutputFolder == "") localOutputFolder = Directory.GetCurrentDirectory();
        }

        using (Process process = new Process())
        {
            process.StartInfo.FileName = filename;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();

            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        outputWaitHandle.Set();
                    }
                    else
                    {
                        output.AppendLine(e.Data);
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
                        error.AppendLine(e.Data);
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
                    exitCode = -1;
                    Console.WriteLine("Error: Process timed out");
                }
                //Either way, write to StdOut and StdErr
                if (output.Length > 0)
                {
                    string stdOutPath = Path.Combine(localOutputFolder, "stdout");
                    File.WriteAllText(stdOutPath, output.ToString());
                }

                if (error.Length > 0)
                {
                    string stdErrPath = Path.Combine(localOutputFolder, "stderr");
                    File.WriteAllText(stdErrPath, error.ToString());
                }

                string retPath = Path.Combine(localOutputFolder, "returncode");
                File.WriteAllText(retPath, process.ExitCode.ToString());
            }
        }

        return exitCode;
    }
}
*/