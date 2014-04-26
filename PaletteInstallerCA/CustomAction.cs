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
using System.Runtime.InteropServices;

namespace PaletteInstallerCA
{
    public class CustomActions
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool DeleteFile(string path);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool RemoveDirectory(string path);

        [CustomAction]
        public static ActionResult CreatePaletteUser(Session session)
        {
            try
            {
                string binDir = session.CustomActionData["INSTALLLOCATION"].ToString();

                string path = binDir + @"\InstallerHelper.exe";

                Process process = new Process();

                process.StartInfo.FileName = path;
                process.StartInfo.Arguments = "disable-uac";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();

                string stdOut = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
            catch (Exception ex)  //catch all exceptions
            {
                //TODO: Write to StdOut, StdErr here, if not, write to Log
                session.Log("Custom Action Exception: " + ex.ToString());
            }

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
            catch (Exception ex)  //catch all exceptions
            {
                //TODO: Write to StdOut, StdErr here, if not, write to Log
                session.Log("Custom Action Exception: " + ex.ToString());
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
            catch (Exception ex)  //catch all exceptions
            {
                //TODO: Write to StdOut, StdErr here, if not, write to Log
                session.Log("Custom Action Exception: " + ex.ToString());
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
            string path = installDir;

            string tableauPath = GetTableauPath(installDir);
            if (tableauPath != null)
                path += Path.PathSeparator.ToString() + Path.Combine(tableauPath, "bin");

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
                output += "uuid=" + System.Guid.NewGuid().ToString() + Environment.NewLine;
                output += "install-dir=" + installDir + Environment.NewLine;
                output += "path=" + path + Environment.NewLine;
                output += Environment.NewLine;
                output += "[controller]" + Environment.NewLine;
                output += "host=" + serverName + Environment.NewLine;
                output += "# port=8888" + Environment.NewLine;
                output += "ssl=true" + Environment.NewLine;
                output += Environment.NewLine;
                output += "[logging]" + Environment.NewLine;
                output += "location=" + installDir + @"logs\agent.log" + Environment.NewLine;
                output += "maxsize=10MB" + Environment.NewLine;

                string inipath = Path.Combine(installDir, @"conf\agent.ini");
                File.WriteAllText(inipath, Convert.ToString(output));
            }
            catch (Exception ex)  //catch all exceptions
            {
                //TODO: Write to StdOut, StdErr here, if not, write to Log
                session.Log("Custom Action Exception: " + ex.ToString());
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
            string userDir = ProgramFilesx86().ToString().Substring(0, 3) + "Users\\Palette";

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
            catch (Exception ex) //if this fails, delete by removing files first
            {
                session.Log("Custom Action Exception: " + ex.ToString());
                if (Directory.Exists(installDir)) DeleteDirectoryWithKernel(installDir, true);
            }

            try
            {
                //Now remove Palette User
                DirectoryEntry localDirectory = new DirectoryEntry("WinNT://" + Environment.MachineName.ToString());
                DirectoryEntries users = localDirectory.Children;
                DirectoryEntry user = users.Find("Palette");
                users.Remove(user);

                if (Directory.Exists(userDir)) Directory.Delete(userDir, true);
            }
            catch //Likely that directory was not removed because it was "not empty" (despite recursive = true)
            {
                try
                {
                    if (Directory.Exists(userDir)) DeleteDirectoryWithKernel(userDir, true);
                }
                catch (Exception ex) //Folder is still there?  Use an asynchronous delete
                {
                    session.Log("Custom Action Exception: " + ex.ToString());
                    if (Directory.Exists(userDir))
                    {
                        try
                        {
                            string binDir = session.CustomActionData["INSTALLLOCATION"].ToString();

                            string path = binDir + @"\InstallerHelper.exe";

                            Process process = new Process();

                            process.StartInfo.FileName = path;
                            process.StartInfo.Arguments = "delete-folder " + userDir;
                            process.StartInfo.UseShellExecute = false;
                            process.StartInfo.CreateNoWindow = true;
                            process.StartInfo.RedirectStandardOutput = true;

                            process.Start();
                        }
                        catch (Exception excep)  //catch all exceptions
                        {
                            //TODO: Write to StdOut, StdErr here, if not, write to Log
                            session.Log("Custom Action Exception: " + excep.ToString());                            
                        }
                    }
                }
            }

            return ActionResult.Success;
        }

        /// <summary>
        /// Delete all files from directory before deleting directory.  Handles Read-Only attributes
        /// OBSOLETE: Use DeleteFolderWithDelay method in InstallerHelper
        /// </summary>
        /// <param name="path">the folder path</param>
        /// <param name="recursive">true for recursive delete</param>
        [Obsolete]
        public static void DeleteDirectoryWithWait(string path, bool recursive)
        {
            // Delete all files and sub-folders?
            if (recursive)
            {
                // Yep... Let's do this
                var subfolders = Directory.GetDirectories(path);
                foreach (var s in subfolders)
                {
                    DeleteDirectoryWithWait(s, recursive);
                }
            }

            // Get all files of the folder
            var files = Directory.GetFiles(path);
            foreach (var f in files)
            {
                // Get the attributes of the file
                var attr = File.GetAttributes(f);

                // Is this file marked as 'read-only'?
                if ((attr & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly)
                {
                    // Yes... Remove the 'read-only' attribute, then
                    File.SetAttributes(f, attr ^ System.IO.FileAttributes.ReadOnly);
                }

                int fileLoopCnt = 0;
                while (File.Exists(f) && fileLoopCnt < 10)
                {
                    try
                    {
                        File.Delete(f);
                    }
                    catch 
                    {
                        Thread.Sleep(100);
                        File.Delete(f);
                    }
                    fileLoopCnt += 1;
                }
            }

            // When we get here, all the files of the folder were
            // already deleted, so we just delete the empty folder

            int dirLoopCnt = 0;
            while (Directory.Exists(path) && dirLoopCnt < 10)
            {
                try
                {
                    Directory.Delete(path);
                }
                catch
                {
                    Thread.Sleep(100);
                    Directory.Delete(path);
                }
                dirLoopCnt += 1;
            }
        }

        /// <summary>
        /// Delete all files from directory before deleting directory using methods from Windows kernel32.dll.  
        /// Handles Read-Only attributes
        /// </summary>
        /// <param name="path">the folder path</param>
        /// <param name="recursive">true for recursive delete</param>
        public static void DeleteDirectoryWithKernel(string path, bool recursive)
        {
            // Delete all files and sub-folders?
            if (recursive)
            {
                // Yep... Let's do this
                var subfolders = Directory.GetDirectories(path);
                foreach (var s in subfolders)
                {
                    DeleteDirectoryWithKernel(s, recursive);
                }
            }

            // Get all files of the folder
            var files = Directory.GetFiles(path);
            foreach (var f in files)
            {
                // Get the attributes of the file
                var attr = File.GetAttributes(f);

                // Is this file marked as 'read-only'?
                if ((attr & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly)
                {
                    // Yes... Remove the 'read-only' attribute, then
                    File.SetAttributes(f, attr ^ System.IO.FileAttributes.ReadOnly);
                }

                // Delete the file
                DeleteFile(f);
            }

            // When we get here, all the files of the folder were
            // already deleted, so we just delete the empty folder
            RemoveDirectory(path);
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
