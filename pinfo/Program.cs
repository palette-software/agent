using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using fastJSON;
using System.Reflection;

class Program
{
    /// <summary>
    /// Writes a serialized JSON string to StdOut with usage information for each drive on the system
    /// </summary>
    /// <param name="args">currently only /du to return disk usage information</param>
    /// <returns>nested JSON</returns>
    static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            //Example: pinfo /du (returns disk usage)
            Console.Error.WriteLine("Usage: pinfo.exe /du");
            return -1;
        }
        else if (args[0] == @"/du")
        {
            Dictionary<string, Dictionary<string, object>> data = GetDriveInfo();

            string json = fastJSON.JSON.Instance.ToJSON(data);

            Console.Out.WriteLine(json);

            return 0;
        }

        else
        {
            Console.Error.WriteLine("Usage: pinfo.exe /du");
            return -1;
        }
    }

    /// <summary>
    /// Returns a nested dictionary of drive info for each drive
    /// </summary>
    /// <returns>a nested dictionary</returns>
    private static Dictionary<string, Dictionary<string, object>> GetDriveInfo()
    {
        //Use a nested dictionary to handle mutliple drives
        Dictionary<string, Dictionary<string, object>> allData = new Dictionary<string, Dictionary<string, object>>();

        DriveInfo[] allDrives = DriveInfo.GetDrives();

        foreach (DriveInfo di in allDrives)
        {
            Dictionary<string, object> driveData = new Dictionary<string, object>();
            driveData["Name"] = di.Name;
            driveData["Drive type"] = di.DriveType;

            if (di.IsReady == true)
            {
                driveData["Volume label"] = di.VolumeLabel;
                driveData["Drive format"] = di.DriveFormat;
                driveData["Available space to current user"] = di.AvailableFreeSpace;
                driveData["Total available space"] = di.TotalFreeSpace;
                driveData["Total size of drive"] = di.TotalSize;
            }

            allData.Add(di.Name, driveData);
        }

        return allData;
    }
}