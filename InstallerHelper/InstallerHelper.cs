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
            case "uuid":
                printUUID();
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

    private static void printUUID()
    {
        string uuid = RegistryUtil.GetPaletteUUID();
        if (uuid != null)
        {
            Console.WriteLine(uuid);
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
}
