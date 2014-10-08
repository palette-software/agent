using System;
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

using SLID = System.Guid;

class pinfo
{
    /// <summary>
    /// Writes a serialized JSON string to StdOut with usage information for each drive on the system
    /// </summary>
    /// <param name="args">currently only /du to return disk usage information</param>
    /// <returns>nested JSON</returns>
    /// 
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

                string dataDir = GetTableauData(installDir);
                allData.Add("tableau-data-dir", dataDir);
                if (Directory.Exists(dataDir))
                {
                    allData.Add("tableau-data-size", DirSize(dataDir));
                }
            }

            //For PD-3852
            if (Environment.OSVersion.Version.Major >= 6) //Version 6 includes Vista, Server 2008, Windows 7, Windows 8, and Server 2012 
            {
                if (IsGenuineWindows())
                {
                    allData.Add("windows-license", "genuine windows");
                }
                else
                {
                    allData.Add("windows-license", "not genuine windows");
                }
            }
            else
            {
                allData.Add("windows-license", "os not supported");
            }
            //End PD-3852

            string json = fastJSON.JSON.Instance.ToJSON(allData);
            Console.WriteLine(json);
        }
        catch
        {
            return -1;
        }

        return 0;
    }

    public enum SL_GENUINE_STATE
    {
        SL_GEN_STATE_IS_GENUINE = 0,
        SL_GEN_STATE_INVALID_LICENSE = 1,
        SL_GEN_STATE_TAMPERED = 2,
        SL_GEN_STATE_LAST = 3
    }

    [DllImportAttribute("Slwga.dll", EntryPoint = "SLIsGenuineLocal", CharSet = CharSet.None, ExactSpelling = false, SetLastError = false, PreserveSig = true, CallingConvention = CallingConvention.Winapi, BestFitMapping = false, ThrowOnUnmappableChar = false)]
    [PreserveSigAttribute()]
    internal static extern uint SLIsGenuineLocal(ref SLID slid, [In, Out] ref SL_GENUINE_STATE genuineState, IntPtr val3);


    public static bool IsGenuineWindows()
    {
        bool _IsGenuineWindows = false;
        Guid ApplicationID = new Guid("55c92734-d682-4d71-983e-d6ec3f16059f"); //Application ID GUID http://technet.microsoft.com/en-us/library/dd772270.aspx
        SLID windowsSlid = (Guid)ApplicationID;
        try
        {
            SL_GENUINE_STATE genuineState = SL_GENUINE_STATE.SL_GEN_STATE_LAST;
            uint ResultInt = SLIsGenuineLocal(ref windowsSlid, ref genuineState, IntPtr.Zero);
            if (ResultInt == 0)
            {
                _IsGenuineWindows = (genuineState == SL_GENUINE_STATE.SL_GEN_STATE_IS_GENUINE);
            }
            else
            {
                Console.WriteLine("Error getting information {0}", ResultInt.ToString());
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return _IsGenuineWindows;
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

    public static string GetTableauData(string installPath)
    {
        string root = Path.GetPathRoot(installPath).ToUpper();

        /* If installed in C: and "C:/ProgramData/Tableau/Tableau Server" exists, use that regardless of registry. */
        if (root == @"C:\")
        {
            string path = StdPath.Combine(root, "ProgramData", "Tableau", "Tableau Server");
            if (Directory.Exists(path))
            {
                return path;
            }
        }

        /* Look for a registry value. */
        string value = RegistryUtil.GetTableauDataDir(installPath);
        if (value != null)
        {
            /* return the parent of directory of the returned value. */
            string relative = Path.Combine(value, "..");
            return Path.GetFullPath(relative);
        }

        /* Default to the install path. */
        return installPath;
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
