using System;
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
        [CustomAction]
        public static ActionResult CreateIniFile(Session session)
        {
            //System.Diagnostics.Debugger.Launch();

            string serverName = session.CustomActionData["SERVERNAME"].ToString().Trim();

            string installDir = session.CustomActionData["INSTALLLOCATION"].ToString();
            string binDir = installDir + @"\bin";

            string path = installDir + @"bin";

            string tableauPath = GetTableauPath(binDir);
            if (tableauPath != null)
                path += Path.PathSeparator.ToString() + tableauPath + @"\bin";

            try
            {
                string output = "[DEFAULT]" + Environment.NewLine;
                output += "type=primary" + Environment.NewLine;
                output += "# archive=false" + Environment.NewLine;
                output += "uuid=" + System.Guid.NewGuid().ToString() + Environment.NewLine;
                output += "install-dir=" + installDir + Environment.NewLine;
                output += "path=" + path + Environment.NewLine;
                output += Environment.NewLine;
                output += "[controller]" + Environment.NewLine;
                output += "host=" + serverName + Environment.NewLine;
                output += "# port=8888" + Environment.NewLine;
                output += Environment.NewLine;
                output += "[archive]" + Environment.NewLine;
                output += "# listen-port=8889" + Environment.NewLine;
                output += Environment.NewLine;
                output += "[logging]" + Environment.NewLine;
                output += "location=" + installDir + @"log\agent.log" + Environment.NewLine;
                output += "maxsize=10MB" + Environment.NewLine;

                string inipath = Path.Combine(installDir, @"conf\agent.ini");
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
    }
}
