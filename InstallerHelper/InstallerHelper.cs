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
            RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"Software\Tableau");
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
}
