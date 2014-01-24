using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Configuration;
using System.Diagnostics;

namespace Palette
{
    class AgentProcess
    {
    //    public static void SpawnProcess(string binFolder, string processName, string jobName, string arguments, string dataFolder, string outputFolder)
    //    {
    //        Process tableauOperation = new Process();
    //        tableauOperation.StartInfo.FileName = binFolder + processName;
    //        tableauOperation.StartInfo.Arguments = arguments + "tableau_" + jobName + "_" + System.DateTime.Now.ToString("yyyy.mm.dd");
    //        tableauOperation.StartInfo.UseShellExecute = false;
    //        tableauOperation.StartInfo.RedirectStandardOutput = true;
    //        tableauOperation.Start();

    //        string outputFileName = outputFolder + "tableau_" + jobName + "_" + System.DateTime.Now.ToString("yyyy.mm.dd") + ".out";

    //        Console.WriteLine(tableauOperation.StandardOutput.ReadToEnd());

    //        using (FileStream fs = new FileStream(outputFileName, FileMode.CreateNew))
    //        {
    //            using (BinaryWriter w = new BinaryWriter(fs))
    //            {
    //                w.Write(tableauOperation.StandardOutput.ReadToEnd());
    //            }
    //        }

    //        tableauOperation.WaitForExit();
    //    }
    }
}
