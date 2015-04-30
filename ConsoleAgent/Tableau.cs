using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

  /// <summary>
    /// This class contains all the Tableau-specific functionality.
    /// All other agent code should remain Tableau-independent.
    /// </summary>
class Tableau
{
    public int Bitness;
    public string Path;
    public string DataPath;
    public string VersionString;
    public string RegistryKeyPath;

    private static string findLatestVersion(RegistryKey baseKey)
    {
        RegistryKey rk = baseKey.OpenSubKey(@"Software\Tableau");
        if (rk == null) return null;

        List<string> subkeys = new List<string>();
        foreach (string key in rk.GetSubKeyNames())
        {
            if (key.StartsWith("Tableau Server"))
            {
                subkeys.Add(key);
            }
        }
        rk.Close();

        if (subkeys.Count == 0) return null;

        subkeys.Sort();
        string verInfo = subkeys.Last();
        return verInfo.Substring(15).Trim();
    }

    private static string buildRegistryKeyPath(string version)
    {
        return @"Software\Tableau\Tableau Server " + version + @"\Directories";
    }

    private static RegistryKey getRegistryKey(Tableau tabinfo)
    {
        RegistryKey key, baseKey;
        string version;

        if (Environment.Is64BitOperatingSystem)
        {
            baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            version = findLatestVersion(baseKey);
            if (version != null)
            {
                tabinfo.VersionString = version;
                tabinfo.Bitness = 64;
                tabinfo.RegistryKeyPath = buildRegistryKeyPath(version);
                key = baseKey.OpenSubKey(tabinfo.RegistryKeyPath);
                baseKey.Close();
                return key;
            }
            baseKey.Close();
        }

        baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
        version = findLatestVersion(baseKey);
        if (version == null)
        {
            baseKey.Close();
            return null;
        }

        tabinfo.VersionString = version;
        tabinfo.Bitness = 32;
        tabinfo.RegistryKeyPath = buildRegistryKeyPath(version);
        key = baseKey.OpenSubKey(tabinfo.RegistryKeyPath);
        baseKey.Close();
        return key;
    }


    public static Tableau query()
    {
        Tableau tabinfo = new Tableau();
        RegistryKey key = getRegistryKey(tabinfo);
        if (key == null) return null;

        tabinfo.Path = (string)key.GetValue("AppVersion");
        tabinfo.DataPath = (string)key.GetValue("Data");
        return tabinfo;
    }
}