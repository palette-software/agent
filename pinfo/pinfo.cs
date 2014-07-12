﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.IO;
using fastJSON;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
            UInt64 installedMemory = 0;
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();

            if (GlobalMemoryStatusEx(memStatus))
            {
                installedMemory = memStatus.ullTotalPhys;
            }

            List<Dictionary<string, object>> driveData = GetDriveInfo();
            allData.Add("volumes", driveData);

            string installDir = RegistryUtil.GetTableauInstallPath();
            if (installDir != null && installDir.Length > 0)
            {
                allData.Add("tableau-install-dir", installDir);
            }

            string dataDir = GetTableauDataPath();
            if (dataDir != null && dataDir.Length > 0)
            {
                allData.Add("tableau-data-dir", dataDir);
                allData.Add("tableau-data-size", DirSize(dataDir));
            }

            string json = fastJSON.JSON.Instance.ToJSON(allData);
            Console.WriteLine(json);
        }
        catch
        {
            return -1;
        }

        return 0;
    }

    /// <summary>
    /// Returns a list of dictionaries containing drive info for each drive
    /// </summary>
    /// <returns>a nested dictionary</returns>
    private static List<Dictionary<string, object>> GetDriveInfo()
    {
        //Use a nested dictionary for volumes to handle mutliple drives
        List<Dictionary<string, object>> allData = new List<Dictionary<string, object>>();

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

            allData.Add(driveData);
        }

        return allData;
    }

    public static string GetTableauDataPath()
    {
        string installDir = RegistryUtil.GetTableauInstallPath();
        if (installDir == null)
        {
            return null;
        }

        string root = Path.GetPathRoot(installDir);
        string path = StdPath.Combine(root, "ProgramData", "Tableau", "Tableau Server");
        return path;
    }

    public static long DirSize(DirectoryInfo d)
    {
        long Size = 0;
        // Add file sizes.
        FileInfo[] fis = d.GetFiles();
        foreach (FileInfo fi in fis)
        {
            Size += fi.Length;
        }
        // Add subdirectory sizes.
        DirectoryInfo[] dis = d.GetDirectories();
        foreach (DirectoryInfo di in dis)
        {
            Size += DirSize(di);
        }
        return (Size);
    }

    public static long DirSize(string path)
    {
        return DirSize(new DirectoryInfo(path));
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
        public MEMORYSTATUSEX()
        {
            this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    /// <summary>
    /// Return the first IPv4 address of this system.
    /// FIXME: copied from Agent.cs
    /// </summary>
    /// <returns></returns>
    public static string GetFirstIPAddr()
    {
        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily.ToString() == "InterNetwork")
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }

    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);
}
