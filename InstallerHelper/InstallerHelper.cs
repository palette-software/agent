using System;
using Microsoft.Win32;

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
        string key = GetTableauRegistryKey();
        if (key == null) return;

        RegistryKey rk = Registry.LocalMachine.OpenSubKey(key + @"\Directories");
        if (rk == null) return;

        object value = rk.GetValue("AppVersion");
        if (value == null) return;

        Console.WriteLine(value.ToString());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static string GetTableauRegistryKey()
    {
        try
        {
            RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"Software\Tableau");  //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\SpecialAccounts\UserList
            string[] sk = rk.GetSubKeyNames();

            foreach (string key in sk)
            {
                if (key.Contains("Tableau Server")) return @"Software\Tableau\" + key;
            }
        }
        catch
        {
            return null;
        }
        return null;
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
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString()); 
        }
    }
}
