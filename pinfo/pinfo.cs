using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using fastJSON;
using System.Reflection;
using System.Diagnostics;

class pinfo
{
    /// <summary>
    /// Writes a serialized JSON string to StdOut with usage information for each drive on the system
    /// </summary>
    /// <param name="args">currently only /du to return disk usage information</param>
    /// <returns>nested JSON</returns>
    static int Main(string[] args)
    {
        Dictionary<string, object> allData = new Dictionary<string,object>();

        try
        {
            PerformanceCounter ramInMB = new PerformanceCounter("Memory", "Available MBytes");
            Dictionary<string, Dictionary<string, object>> driveData = GetDriveInfo();

            allData.Add("os-version", System.Environment.OSVersion.ToString());
            allData.Add("processor-type", System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"));
            allData.Add("processor-count", System.Environment.ProcessorCount.ToString());
            allData.Add("installed-memory", Convert.ToUInt64(ramInMB.NextValue()) * 1024 * 1024);
            allData.Add("machine-name", System.Environment.MachineName);
            allData.Add("user-name", System.Environment.UserName);            
            allData.Add("volumes", driveData);

            string path = RegistryUtil.GetTableauInstallPath();
            if (path != null && path.Length > 0)
            {
                allData.Add("tableau-install-dir", path);
            }

            string json = fastJSON.JSON.Instance.ToJSON(allData);
            Console.Out.WriteLine(json);
        }
        catch
        {
            return -1;
        }

        return 0;
    }

    /// <summary>
    /// Returns a nested dictionary of drive info for each drive
    /// </summary>
    /// <returns>a nested dictionary</returns>
    private static Dictionary<string, Dictionary<string, object>> GetDriveInfo()
    {
        //Use a nested dictionary for volumes to handle mutliple drives
        Dictionary<string, Dictionary<string, object>> allData = new Dictionary<string, Dictionary<string, object>>();

        DriveInfo[] allDrives = DriveInfo.GetDrives();

        foreach (DriveInfo di in allDrives)
        {
            Dictionary<string, object> driveData = new Dictionary<string, object>();
            string[] tokens = di.Name.Split(':');
            driveData["name"] = tokens[0];
            driveData["type"] = di.DriveType;

            if (di.IsReady == true)
            {
                driveData["label"] = di.VolumeLabel;
                driveData["drive-format"] = di.DriveFormat;                
                driveData["available-space"] = di.TotalFreeSpace;
                driveData["size"] = di.TotalSize;
            }

            allData.Add(di.Name, driveData);
        }

        return allData;
    }
}