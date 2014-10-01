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
                return printTableauInstallPath();
            case "hide-user":
                return HideUser();
            case "disable-uac":
                return DisableUAC();
            case "enable-uac":
                return EnableUAC();
            case "uuid":
                return printUUID();
        }

        Usage();
        return -1;
    }

    /// <summary>
    /// 
    /// </summary>
    private static void Usage()
    {
        Console.Error.WriteLine("Usage: InstallerHelper <action> [options]\n");
        // FIXME: fill in the options.
    }

    private static int printTableauInstallPath()
    {
        string path = RegistryUtil.GetTableauInstallPath();
        if (path != null)
        {
            Console.WriteLine(path);
        }
        return 0;
    }

    private static int printUUID()
    {
        string uuid = RegistryUtil.GetPaletteUUID();
        if (uuid != null)
        {
            Console.WriteLine(uuid);
        }
        return 0;
    }

    public static int DisableUAC()
    {
        try
        {
            RegistryKey rk = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", true);
            object obj1 = rk.GetValue("EnableLUA");
            bool uacEnabled = Convert.ToBoolean(obj1);

            if (uacEnabled == true)
            {
                rk.SetValue("EnableLUA", 0, RegistryValueKind.DWord);

                //Note in registry that setting has been changed
                string userRoot = "HKEY_CURRENT_USER";
                string subkey = "SOFTWARE\\Palette\\UAC";
                string keyName = userRoot + "\\" + subkey;
                Registry.SetValue(keyName, "UACChanged", 1);

                return -1;
            }
            else
            {
                //Note in registry that setting has NOT been changed
                string userRoot = "HKEY_CURRENT_USER";
                string subkey = "SOFTWARE\\Palette\\UAC";
                string keyName = userRoot + "\\" + subkey;
                Registry.SetValue(keyName, "UACChanged", 0);
            }
        }
        catch (Exception e) //catch all exceptions
        {
            Console.Error.WriteLine(e.ToString());
            return -1;
        }
        return 0;
    }

    public static int EnableUAC()
    {
        try
        {
            //First find out if UAC Registry setting was changed
            RegistryKey reg = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Palette\\UAC", true);
            int wasChanged = Convert.ToInt16(reg.GetValue("UACChanged"));

            //Delete Keys. Cannot be done recursively
            string subkey = "SOFTWARE\\Palette\\UAC";
            Registry.CurrentUser.DeleteSubKey(subkey);

            string superkey = "SOFTWARE\\Palette";
            Registry.CurrentUser.DeleteSubKey(superkey);

            RegistryKey rk = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", true);
            object obj1 = rk.GetValue("EnableLUA");
            bool uacEnabled = Convert.ToBoolean(obj1);

            if (uacEnabled == false && wasChanged == 1)
            {
                rk.SetValue("EnableLUA", 1, RegistryValueKind.DWord);
                return -1;
            }
        }
        catch (Exception e) //catch all exceptions
        {
            Console.Error.WriteLine(e.ToString());
            return -1;
        }
        return 0;
    }

    public static int HideUser()
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
        catch (Exception e)//catch all exceptions
        {
            Console.Error.WriteLine(e.ToString());
            return -1;
        }
        return 0;
    }
}
