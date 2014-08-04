using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;

class RegistryUtil
{
    public static string GetTableauKey()
    {
        string value = null;
        RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"Software\Tableau");
        if (rk == null) return null;

        foreach (string key in rk.GetSubKeyNames())
        {
            if (key.Contains("Tableau Server")) value = @"Software\Tableau\" + key;
        }
        rk.Close();
        return value;
    }

    private static string GetTableauValueByKey(string key)
    {
        string parent = GetTableauKey();
        if (parent == null) return null;

        RegistryKey rk = Registry.LocalMachine.OpenSubKey(parent + @"\Directories");
        if (rk == null) return null;

        object value = rk.GetValue(key);
        rk.Close();

        return value != null ? value.ToString() : null;
    }

    public static string GetTableauInstallPath()
    {
        return GetTableauValueByKey("AppVersion");
    }

    public static string GetTableauDataDir(string installPath)
    {
        return GetTableauValueByKey("Data");
    }

    public static string GetPaletteUUID()
    {
        RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"Software\Palette");
        if (rk == null)
        {
            rk = Registry.LocalMachine.CreateSubKey(@"Software\Palette");
        }

        if (rk == null) throw new Exception("Could not create 'Palette' registry key.");

        object obj = rk.GetValue("UUID");
        if (obj == null)
        {
            string uuid = System.Guid.NewGuid().ToString();
            rk.SetValue("UUID", uuid);
            return uuid;
        }
        return (string)obj;
    }
}
