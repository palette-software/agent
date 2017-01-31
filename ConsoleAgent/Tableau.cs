using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Text.RegularExpressions;

/// <summary>
/// This class contains all the Tableau-specific functionality.
/// All other agent code should remain Tableau-independent.
/// </summary>
public class Tableau
{
    public const string YML_READONLY_ENABLED = "pgsql.readonly.enabled";
    public const string YML_SYSINFO_IPS = "wgserver.systeminfo.allow_referrer_ips";
    public const string MINIMUM_SUPPORTED_VERSION = "8.2.5";

    public int Bitness;
    public string Path;
    public string DataPath;
    public string RegistryKeyPath;
    public string VersionString;
    public Version Version;

    /// <summary>
    /// Get the tabadmin path and throw and exception if it isn't where its supposed to be.
    /// </summary>
    /// <returns></returns>
    protected string tabadminPath()
    {
        string path = System.IO.Path.Combine(Path, "bin", "tabadmin.exe");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("File not found: " + path, path);
        }
        return path;
    }

    public void tabadminRun(string arguments)
    {
        string tabadmin = tabadminPath();

        Process process = new Process();

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.FileName = tabadmin;
        process.StartInfo.Arguments = arguments;

        process.Start();

        /* tabadmin seems to only use stdout */
        string error = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception("tabadmin failed: " + arguments + ", ExitCode=" + Convert.ToString(process.ExitCode) + "\n" + error);
        }
    }

    private static string findLatestVersion(RegistryKey baseKey)
    {
        RegistryKey tableauKey = baseKey.OpenSubKey(@"Software\Tableau");
        if (tableauKey == null)
        {
            return null;
        }

        Version latestTableauVersion = new Version("0.0");

        foreach (string key in tableauKey.GetSubKeyNames())
        {
            var pattern = new Regex(@"^Tableau Server ([0-9]+\.[0-9]+)");
            var groups = pattern.Match(key).Groups;
            // groups[0] is the entire match, thus we expect 2
            if (groups.Count < 2)
            {
                continue;
            }

            Version version = new Version(groups[1].Value);
            if (version > latestTableauVersion)
            {
                try
                {
                    string directoriesRegPath = System.IO.Path.Combine(key, "Directories");
                    using (RegistryKey dataFolderKey = tableauKey.OpenSubKey(directoriesRegPath))
                    {
                        if (dataFolderKey == null)
                        {
                            continue;
                        }

                        latestTableauVersion = version;
                    }
                }
                catch (Exception)
                {
                    // no problem, only means this is not our version
                    continue;
                }
            }
        }
        tableauKey.Close();

        if (latestTableauVersion.Equals(new Version("0.0")))
        {
            return null;
        }

        return latestTableauVersion.ToString();
    }

    private static string buildRegistryKeyPath(string version)
    {
        return @"Software\Tableau\Tableau Server " + version + @"\Directories";
    }

    private static RegistryKey getRegistryKey(Tableau tabinfo)
    {
        RegistryKey key, baseKey;
        string version, rkPath;

        if (Environment.Is64BitOperatingSystem)
        {
            baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            version = findLatestVersion(baseKey);
            if (version != null)
            {
                rkPath = buildRegistryKeyPath(version);
                key = baseKey.OpenSubKey(rkPath);

                if (key.GetValue("AppVersion") != null)
                {
                    tabinfo.VersionString = version;
                    Version.TryParse(version, out tabinfo.Version);
                    tabinfo.Bitness = 64;
                    tabinfo.RegistryKeyPath = rkPath;
                    baseKey.Close();
                    return key;
                }
                else
                {
                    key.Close();
                }
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

        rkPath = buildRegistryKeyPath(version);
        key = baseKey.OpenSubKey(rkPath);
        baseKey.Close();

        if (key.GetValue("AppVersion") == null)
        {
            key.Close();
            return null;
        }

        tabinfo.VersionString = version;
        Version.TryParse(version, out tabinfo.Version);
        tabinfo.Bitness = 32;
        tabinfo.RegistryKeyPath = rkPath;

        return key;
    }


    public static Tableau query()
    {
        Tableau tabinfo = new Tableau();
        RegistryKey key = getRegistryKey(tabinfo);
        if (key == null) return null;

        tabinfo.Path = (string)key.GetValue("AppVersion");
        tabinfo.DataPath = (string)key.GetValue("Data");
        if (!Directory.Exists(tabinfo.DataPath))
        {
            string path = @"C:\ProgramData\Tableau\Tableau Server\data";
            if (Directory.Exists(path))
            {
                tabinfo.DataPath = path;
            }
        }
        return tabinfo;
    }

    public Dictionary<string, string> getSettings()
    {
        String path = System.IO.Path.Combine(DataPath, "tabsvc", "config", "workgroup.yml");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("workgroup.yml");
        }

        Dictionary<string, string> dict = new Dictionary<string, string>();

        using (StreamReader reader = new StreamReader(path))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0 || line == "---")
                {
                    continue;
                }
                string [] array = line.Split(":".ToCharArray(), 2);
                if (array.Length == 1)
                {
                    // shouldn't happen.
                    dict[array[0].Trim()] = null;
                }
                else
                {
                    dict[array[0].Trim()] = array[1].Trim();
                }
            }
        }
        return dict;
    }

    public void enableReadonlyUser(string password)
    {
        tabadminRun("dbpass --username readonly " + password);
    }

    public void enableSysInfo(string[] ips)
    {
        string value;
        if (ips == null)
        {
            value = "127.0.0.1";
        }
        else if (ips.Contains("127.0.0.1"))
        {
            return;
        }
        else
        {
            value = "127.0.0.1," + String.Join(",", ips);
        }
        tabadminRun("set " + YML_SYSINFO_IPS + " " + value);
    }


    public static bool readOnlyEnabled(Dictionary<string, string> settings)
    {
        try
        {
            return Convert.ToBoolean(settings[Tableau.YML_READONLY_ENABLED]);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static string[] allowedSysInfoIPs(Dictionary<string, string> settings)
    {
        if (!settings.ContainsKey(Tableau.YML_SYSINFO_IPS))
        {
            return null;
        }
        string value = settings[Tableau.YML_SYSINFO_IPS].Trim();
        if (value.ToLower() == "false") {
            return null;
        }
        string[] tokens = value.Split(",".ToCharArray());

        List<string> list = new List<string>(tokens.Length);

        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i].Trim();
            if (token.Length > 0)
            {
                list.Add(token);
            }
        }
        if (list.Count == 0)
        {
            return null;
        }
        return list.ToArray();
    }

    /// <summary>
    /// Restarts Tableau Server using tabadmin.
    /// </summary>
    public void restart()
    {
        tabadminRun("restart");
    }

    /// <summary>
    /// Stops Tableau Server using tabadmin.
    /// </summary>
    public void stop()
    {
        tabadminRun("stop");
    }

    /// <summary>
    /// Start Tableau Server using tabadmin.
    /// </summary>
    public void start()
    {
        tabadminRun("start");
    }

    /// <summary>
    /// Runs tabadmin configure.
    /// </summary>
    public void configure()
    {
        tabadminRun("configure");
    }

}