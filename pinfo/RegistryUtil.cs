using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

class RegistryUtil
{
    public static string GetTableauKey()
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
        catch //catch all exceptions
        {
        }
        return null;
    }

    public static string GetTableauInstallPath()
    {
        string key = GetTableauKey();
        if (key == null) return null;

        RegistryKey rk = Registry.LocalMachine.OpenSubKey(key + @"\Directories");
        if (rk == null) return null;

        object value = rk.GetValue("AppVersion");
        if (value == null) return null;

        return value.ToString();
    }
}
