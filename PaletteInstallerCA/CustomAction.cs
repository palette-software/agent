﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.IO;
using System.Threading;
using System.ServiceProcess;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Win32;

namespace PaletteInstallerCA
{
    public class CustomActions
    {
        //[CustomAction]
        //public static ActionResult UpdateIniWithServerName(Session session)
        //{
        //    try
        //    {
        //        string output = "[DEFAULT]" + Environment.NewLine;
        //        output += "type=primary" + Environment.NewLine;
        //        output += "# archive=false" + Environment.NewLine;
        //        output += "uuid=5e94aad1-67bc-46b7-a16d-52c6b1443cef" + Environment.NewLine;
        //        output += "install-dir=" + session["INSTALLLOCATION"].ToString() + Environment.NewLine; //session["INSTALLLOCATION"].ToString() = C:\Program Files (x86)\Palette\
        //        output += "# hostname=primary" + Environment.NewLine;
        //        output += Environment.NewLine;
        //        output += "[controller]" + Environment.NewLine;
        //        output += "# host=" + session["SERVERNAME"].ToString() + Environment.NewLine;
        //        output += "# port=8888" + Environment.NewLine;
        //        output += Environment.NewLine;
        //        output += "[archive]" + Environment.NewLine;
        //        output += "# listen-port=8889" + Environment.NewLine;
        //        output += Environment.NewLine;
        //        output += "[logging]" + Environment.NewLine;
        //        output += "# location=" + session["INSTALLLOCATION"].ToString() + @"log\agent.log" + Environment.NewLine;
        //        output += "maxsize=10MB" + Environment.NewLine;                   

        //        string path = Path.Combine("c://temp//", "agent.ini");
        //        File.WriteAllText(path, Convert.ToString(output));
        //    }
        //    catch
        //    {
        //        return ActionResult.Failure;
        //    }

        //    return ActionResult.Success;
        //}

        [CustomAction]
        public static ActionResult CreateIniFile(Session session)
        {
            System.Diagnostics.Debugger.Launch();
            string path = session["INSTALLLOCATION"].ToString() + @"bin";

            string tableauPath = GetTableauPath(session);
            if (tableauPath != null)
                path += Path.PathSeparator.ToString() + tableauPath + @"\bin";

            try
            {
                string output = "[DEFAULT]" + Environment.NewLine;
                output += "type=primary" + Environment.NewLine;
                output += "# archive=false" + Environment.NewLine;
                output += "uuid=" + System.Guid.NewGuid().ToString() + Environment.NewLine;
                output += "install-dir=" + session["INSTALLLOCATION"].ToString() + Environment.NewLine;
                output += "path=" + path + Environment.NewLine;
                output += Environment.NewLine;
                output += "[controller]" + Environment.NewLine;
                output += "host=" + session["SERVERNAME"].ToString().Trim() + Environment.NewLine;
                output += "# port=8888" + Environment.NewLine;
                output += Environment.NewLine;
                output += "[archive]" + Environment.NewLine;
                output += "# listen-port=8889" + Environment.NewLine;
                output += Environment.NewLine;
                output += "[logging]" + Environment.NewLine;
                output += "location=" + session["INSTALLLOCATION"].ToString() + @"log\agent.log" + Environment.NewLine;
                output += "maxsize=10MB" + Environment.NewLine;

                string inipath = Path.Combine(session["INSTALLLOCATION"].ToString(), @"conf\agent.ini");
                File.WriteAllText(inipath, Convert.ToString(output));
            }
            catch
            {
                return ActionResult.Failure;
            }

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult CleanupAfterUnInstall(Session session)
        {
            session.Log("Begin CleanupAfterInstall");

            //First try the default installation path
            string installDir = ProgramFilesx86() + "\\Palette\\";

            try
            {
                //if not there, check the registry to see where it was put
                if (!Directory.Exists(installDir))
                {
                    RegistryKey rk = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\services\\Palette");
                    installDir = rk.GetValue("ImagePath").ToString().TrimStart('"').TrimEnd('"');
                    installDir = installDir.Replace("ServiceAgent.exe", "");
                }

                if (Directory.Exists(installDir)) Directory.Delete(installDir, true);
            }
            catch //Catch all exceptions
            {
            }

            return ActionResult.Success;
        }

        /// <summary>
        /// The function below will return the x86 Program Files directory in all of these three Windows configurations:
        ///   32 bit Windows, 32 bit program running on 64 bit Windows, 64 bit program running on 64 bit windows
        /// </summary>
        /// <returns>x86 Program Files directory</returns>
        private static string ProgramFilesx86()
        {
            if (8 == IntPtr.Size
                || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }

            return Environment.GetEnvironmentVariable("ProgramFiles");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string GetTableauRegistryKey()
        {
            RegistryKey rk = Registry.LocalMachine.OpenSubKey("Software\\Tableau");
            if (rk == null) return null;

            string[] sk = rk.GetSubKeyNames();

            foreach (string key in sk)
            {
                if (key.Contains("Tableau Server")) return key;
            }
            return sk.ToString();
        }

        /// <summary>
        /// Finds out if Tableau is installed by querying the registry.
        /// </summary>
        /// <returns>the install path or null if not found.</returns>
        public static string GetTableauPath(string binDir)
        {
            //System.Diagnostics.Debugger.Launch();
            string path = binDir + @"\InstallerHelper.exe";

            Process process = new Process();

            process.StartInfo.FileName = path;
            process.StartInfo.Arguments = "tableau-install-path";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;

            process.Start();

            string stdOut = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return stdOut.Trim();
        }

        public static string GetTableauPath(Session session)
        {
            return GetTableauPath(session["INSTALLLOCATION"].ToString() + @"\bin");
        }

    }
}
