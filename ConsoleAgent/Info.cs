using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.IO;
using System.Runtime.InteropServices;
using SLID = System.Guid;

class Info
{
    public static Dictionary<string, object> Generate(Tableau tabinfo)
    {
        Dictionary<string, object> data = new Dictionary<string, object>();

        UInt64 installedMemory = 0;
        MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();

        if (GlobalMemoryStatusEx(memStatus))
        {
            installedMemory = memStatus.ullTotalPhys;
        }

        string hostname = Dns.GetHostName();
        data.Add("hostname", hostname);
        data.Add("fqdn", NetUtil.GetFQDN(hostname));
        data.Add("ip-address", NetUtil.GetFirstIPAddr(hostname));
        data.Add("volumes", GetDriveInfo());

        if (tabinfo != null)
        {
            string tableauInstallDir = tabinfo.Path;
            if (tableauInstallDir != null && tableauInstallDir.Length > 0)
            {
                data.Add("tableau-install-dir", tableauInstallDir);

                string tableauDataDir = tabinfo.DataPath;
                data.Add("tableau-data-dir", tableauDataDir);
                if (Directory.Exists(tableauDataDir))
                {
                    data.Add("tableau-data-size", DirSize(tableauDataDir));
                }
            }
        }

        //For PD-3852
        if (Environment.OSVersion.Version.Major >= 6) //Version 6 includes Vista, Server 2008, Windows 7, Windows 8, and Server 2012 
        {
            if (IsGenuineWindows())
            {
                data.Add("windows-license", "genuine windows");
            }
            else
            {
                data.Add("windows-license", "not genuine windows");
            }
        }
        else
        {
            data.Add("windows-license", "os not supported");
        }
        return data;
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

            try
            {
                driveData["label"] = di.VolumeLabel;
                driveData["drive-format"] = di.DriveFormat;
                driveData["available-space"] = di.TotalFreeSpace;
                driveData["size"] = di.TotalSize;
            }
            catch (IOException)
            {
            }

            allData.Add(driveData);
        }

        return allData;
    }

    public static long DirSize(DirectoryInfo d)
    {
        long Size = 0;
        try
        {
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
        }
        catch (IOException)
        {
            // It's possible there is a bad subdir or something we don't have permission for,
            // just exclude such values from the total size.
        }
        return Size;
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
