using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

public class ThreadProcess
{
    private Process process;
    string outputFileName;  //full path + filename

    public ThreadProcess(Process process, string outputFileName)
    {
        this.process = process;
        this.outputFileName = outputFileName;
    }

    public void SpawnThreadProcess()
    {
        try
        {
            process.Start();

            using (StreamWriter writer = new StreamWriter(outputFileName))
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
