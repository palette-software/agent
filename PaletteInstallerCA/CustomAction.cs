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
using System.DirectoryServices;
using Microsoft.Win32;

namespace PaletteInstallerCA
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult CreatePaletteUser(Session session)
        {
            //string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            try
            {
                string userName = "Palette";
                DirectoryEntry AD = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer");
                DirectoryEntry NewUser = AD.Children.Add(userName, "user");
                string pwd = CreatePassword(10); 
                NewUser.Invoke("SetPassword", new object[] { pwd });
                NewUser.Invoke("Put", new object[] { "Description", "Palette User for Agent Service" });
                NewUser.CommitChanges();
                DirectoryEntry grp;
                grp = AD.Children.Find("Administrators", "group");
                if (grp != null) { grp.Invoke("Add", new object[] { NewUser.Path.ToString() }); }

                GrantLogonAsServiceRight(userName);

                session.CustomActionData["SERVICEACCOUNT"] = Environment.MachineName + "\\" + userName;
                session.CustomActionData["SERVICEPASSWORD"] = pwd;
                session["SERVICEACCOUNT"] = Environment.MachineName + "\\" + userName;
                session["SERVICEPASSWORD"] = pwd;
            }
            catch  //catch all exceptions
            {
                return ActionResult.Failure;
            }

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult HidePaletteUser(Session session)
        {
            try
            {
                string binDir = session.CustomActionData["INSTALLLOCATION"].ToString();

                string path = binDir + @"\InstallerHelper.exe";

                Process process = new Process();

                process.StartInfo.FileName = path;
                process.StartInfo.Arguments = "hide-user";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();

                string stdOut = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
            catch  //catch all exceptions
            {
                return ActionResult.Failure;
            }

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult CreateIniFile(Session session)
        {
            //System.Diagnostics.Debugger.Launch();

            string serverName = session.CustomActionData["SERVERNAME"].ToString().Trim();

            string installDir = session.CustomActionData["INSTALLLOCATION"].ToString();
            string binDir = installDir;
            string path = installDir;

            string tableauPath = GetTableauPath(binDir);
            if (tableauPath != null)
                path += Path.PathSeparator.ToString() + tableauPath;

            try
            {
                string output = "[DEFAULT]" + Environment.NewLine;
                if (tableauPath != null)
                {
                    output += "type=primary" + Environment.NewLine;
                }
                else
                {
                    output += "type=other" + Environment.NewLine;
                }
                output += "# archive=false" + Environment.NewLine;
                output += "uuid=" + System.Guid.NewGuid().ToString() + Environment.NewLine;
                output += "install-dir=" + installDir + Environment.NewLine;
                output += "path=" + path + Environment.NewLine;
                output += Environment.NewLine;
                output += "[controller]" + Environment.NewLine;
                output += "host=" + serverName + Environment.NewLine;
                output += "# port=8888" + Environment.NewLine;
                output += "ssl=true" + Environment.NewLine;
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
            }
            catch //Catch all exceptions
            {
                //return ActionResult.Failure;
            }
            finally
            {
                if (Directory.Exists(installDir)) Directory.Delete(installDir, true);
            }

            try
            {
                //Now remove Palette User
                DirectoryEntry localDirectory = new DirectoryEntry("WinNT://" + Environment.MachineName.ToString());
                DirectoryEntries users = localDirectory.Children;
                DirectoryEntry user = users.Find("Palette");
                users.Remove(user);

                //TODO: Make sure user folder is being removed
            }
            catch //Catch all exceptions
            {
                return ActionResult.Failure;
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

            string tableauPath = stdOut.Trim();
            if (tableauPath.Length == 0)
            {
                return null;
            }
            return tableauPath;
        }

        private static string CreatePassword(int length)
        {
            string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            string res = "";
            Random rnd = new Random();
            while (0 < length--)
                res += valid[rnd.Next(valid.Length)];
            return res;
        }

        private static void GrantLogonAsServiceRight(string username)
        {
            using (LsaWrapper lsa = new LsaWrapper())
            {
                lsa.AddPrivileges(username, "SeServiceLogonRight");
            }
        }
    }
}
