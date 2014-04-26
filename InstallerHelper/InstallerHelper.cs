using System;
using Microsoft.Win32;
using System.Threading;
using System.IO;

class InstallerHelper
{
    static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Usage();
            return -1;
        }

        string action = args[0].ToLower();

        switch (action)
        {
            case "tableau-install-path":
                printTableauInstallPath();
                break;
            case "hide-user":
                HideUser();
                break;
            case "disable-uac":
                DisableUAC();
                break;
            case "delete-folder":
                DeleteFolderWithDelay(args[1]);
                break;
            default:
                Usage();
                return -1;
        }
        return 0;
    }

    /// <summary>
    /// 
    /// </summary>
    private static void Usage()
    {
        Console.Error.WriteLine("Usage: InstallerHelper <action> [options]\n");
        // FIXME: fill in the options.
    }

    private static void printTableauInstallPath()
    {
        string path = RegistryUtil.GetTableauInstallPath();
        if (path != null)
        {
            Console.WriteLine(path);
        }
    }

    public static string DisableUAC()
    {
        try
        {
            RegistryKey rk = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", true);
            object obj1 = rk.GetValue("EnableLUA");
            bool uacEnabled = Convert.ToBoolean(obj1);

            if (uacEnabled == true) rk.SetValue("EnableLUA", 0, RegistryValueKind.DWord);

            return uacEnabled.ToString();
        }
        catch //catch all exceptions
        {
            return null;
        }
    }

    public static void EnableUAC()
    {
        try
        {
            RegistryKey rk = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", true);
            object obj1 = rk.GetValue("EnableLUA");
            bool uacEnabled = Convert.ToBoolean(obj1);

            if (uacEnabled == false) rk.SetValue("EnableLUA", 1, RegistryValueKind.DWord);
        }
        catch //catch all exceptions
        {
        }
    }

    public static void HideUser()
    {
        string userName = "Palette";

        try
        {
            //HKEY_LOCAL_MACHINE\Software\Microsoft\Windows NT\CurrentVersion\Winlogon  \SpecialAccounts\UserList
            string key = @"Software\Microsoft\Windows NT\CurrentVersion\Winlogon\SpecialAccounts";

            RegistryKey saSubKey = Registry.LocalMachine.OpenSubKey(key, true);
            if (saSubKey == null)
            {
                // It doesn't exist
                saSubKey = Registry.LocalMachine.CreateSubKey(key);
                RegistryKey ulSubKey = saSubKey.CreateSubKey("UserList");
                ulSubKey.SetValue(userName, 0, RegistryValueKind.DWord);
            }
            else
            {
                // It exists
                RegistryKey ulSubKey = saSubKey.OpenSubKey("UserList");
                if (ulSubKey == null)
                {
                    // It doesn't exist
                    ulSubKey = saSubKey.CreateSubKey("UserList");
                    ulSubKey.SetValue(userName, 0, RegistryValueKind.DWord);
                }
                else
                {
                    // It exists
                    ulSubKey = saSubKey.OpenSubKey("UserList", true);
                    ulSubKey.SetValue(userName, 0, RegistryValueKind.DWord);
                }
            }
        }
        catch //catch all exceptions
        {
        }
    }

    /// <summary>
    /// Delete folder with a delay, meant for asynchrounous use on a locked folder
    /// </summary>
    /// <param name="folderPath">folder path</param>
    public static void DeleteFolderWithDelay(string folderPath)
    {
        Thread.Sleep(20000);
        try
        {
            //Now remove the folder
            if (Directory.Exists(folderPath)) Directory.Delete(folderPath, true);
        }
        catch //Likely that directory was not removed because it was "not empty" (despite recursive = true)
        {
            DeleteDirectoryRecursively(folderPath, true);
        }
    }

    /// <summary>
    /// Delete all files from directory before deleting directory  
    /// Handles Read-Only attributes
    /// </summary>
    /// <param name="path">the folder path</param>
    /// <param name="recursive">true for recursive delete</param>
    public static void DeleteDirectoryRecursively(string path, bool recursive)
    {
        // Delete all files and sub-folders?
        if (recursive)
        {
            // Yep... Let's do this
            var subfolders = Directory.GetDirectories(path);
            foreach (var s in subfolders)
            {
                DeleteDirectoryRecursively(s, recursive);
            }
        }

        // Get all files of the folder
        var files = Directory.GetFiles(path);
        foreach (var f in files)
        {
            // Get the attributes of the file
            var attr = File.GetAttributes(f);

            // Is this file marked as 'read-only'?
            if ((attr & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly)
            {
                // Yes... Remove the 'read-only' attribute, then
                File.SetAttributes(f, attr ^ System.IO.FileAttributes.ReadOnly);
            }

            // Delete the file
            File.Delete(f);
        }

        // When we get here, all the files of the folder were
        // already deleted, so we just delete the empty folder
        Directory.Delete(path);
    }
}
