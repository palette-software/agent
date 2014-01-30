using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

public class ThreadProcess
{
    private Process process;
    string stdOutFile;  //full path + filename
    string stdErrFile;

    public ThreadProcess(Process process, string fullOutputPath)
    {
        this.process = process;
        this.stdOutFile = fullOutputPath + "stdout.txt";
        this.stdErrFile = fullOutputPath + "stderr.txt";
    }

    public void SpawnThreadProcess()
    {
        try
        {
            process.Start();

            //TODO: Need to deal with stderr
            using (StreamWriter writer = new StreamWriter(stdOutFile))
            {
                writer.Write(process.StandardOutput.ReadToEnd());

                process.WaitForExit();
            }
        }

        catch (Exception exc)
        {
            Console.WriteLine(exc.ToString());
        }
    }
}
