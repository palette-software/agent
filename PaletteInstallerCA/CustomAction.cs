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
        public static ActionResult DisableUAC(Session session)
        {
            session.Log("Starting custom action DisableUAC with Session Variables INSTALLOCATION: "
                    + session.CustomActionData["INSTALLLOCATION"].ToString());

            //System.Diagnostics.Debugger.Launch();

            try
            {
                string binDir = session.CustomActionData["INSTALLLOCATION"].ToString();

                string path = StdPath.Combine(binDir, "InstallerHelper.exe");

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
                return ActionResult.Failure;
            }

            session.Log("Successfully finished custom action DisableUAC");
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult DeletePaletteUser(Session session)
        {
            session.Log("Starting custom action DeletePaletteUser");
            //System.Diagnostics.Debugger.Launch();

            try
            {
                int result = DeleteUser("Palette", session);

                if (result != 0) session.Log("Existing User Account not removed properly");
            }
            catch (Exception ex)  //catch all exceptions
            {
                //TODO: Write to StdOut, StdErr here, if not, write to Log
                session.Log("Custom Action Exception: " + ex.ToString());

                return ActionResult.Failure;
            }

            session.Log("Successfully finished action DeletePaletteUser");
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult CreatePaletteUser(Session session)
        {
            session.Log("Starting custom action CreatePaletteUser");

            //System.Diagnostics.Debugger.Launch();

            try
            {
                string userName = "Palette";
                DirectoryEntry AD = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer");
                DirectoryEntry NewUser = AD.Children.Add(userName, "user");
                string pwd = CreatePassword(12);
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
            catch (Exception ex)  //catch all exceptions.  These are show stoppers.
            {
                //TODO: Write to StdOut, StdErr here, if not, write to Log
                session.Log("Custom Action Exception: " + ex.ToString());
                return ActionResult.Failure;
            }

            //session.Log("Attempting to hide Palette user folder");

            //string drive = Directory.GetDirectoryRoot(ProgramFilesx86());
            //string startDir = StdPath.Combine(drive, "Users", "Palette");

            //session.Log("Hiding folders and files in " + startDir);

            //if (!Directory.Exists(startDir)) Thread.Sleep(60000);

            //if (Directory.Exists(startDir))
            //{
            //    if (HideFolders(startDir, false))
            //    {
            //        session.Log("Exception in method Hidefolders()");
            //    }
            //    else
            //    {
            //        session.Log("Successfully finished action HidePaletteUser");
            //    }
            //}
            //else
            //{
            //    session.Log("No Palette user folder found");
            //}

            session.Log("Successfully finished action CreatePaletteUser");
            return ActionResult.Success;
        }


        [CustomAction]
        public static ActionResult HidePaletteUser(Session session)
        {
            session.Log("Starting custom action HidePaletteUser");

            string drive = Directory.GetDirectoryRoot(ProgramFilesx86());
            string startDir = StdPath.Combine(drive, "Users", "Palette");

            session.Log("Hiding folders and files in " + startDir);

            if (Directory.Exists(startDir))
            {
                if (HideTopLevelFolder(startDir, false))
                {
                    session.Log("Exception in method Hidefolders()");
                }
                else
                {
                    session.Log("Successfully finished action HidePaletteUser");
                }
            }
            else
            {
                session.Log("No Palette user folder found");
            }
            return ActionResult.Success;
        }

        /// <summary>
        /// Makes a folder and its contents hidden
        /// </summary>
        /// <param name="startDir">the top level directory to make hidden</param>
        public static bool HideTopLevelFolder(string startDir, bool error)
        {
            DirectoryInfo dir = new DirectoryInfo(startDir);

            // First, set hidden flag on the current directory (if needed)
            if ((dir.Attributes & System.IO.FileAttributes.Hidden) == 0)
            {
                try
                {
                    File.SetAttributes(dir.FullName, File.GetAttributes(dir.FullName) | System.IO.FileAttributes.Hidden);
                }
                catch
                {
                    error = true;
                }
            }

            return error;
        }

        /// <summary>
        /// Recursively makes a folder and its contents hidden
        /// </summary>
        /// <param name="startDir">the top level directory to make hidden</param>
        public static bool HideFolders(string startDir, bool error)
        {
            DirectoryInfo dir = new DirectoryInfo(startDir);

            // First, set hidden flag on the current directory (if needed)
            if ((dir.Attributes & System.IO.FileAttributes.Hidden) == 0)
            {
                try
                {
                    File.SetAttributes(dir.FullName, File.GetAttributes(dir.FullName) | System.IO.FileAttributes.Hidden);
                }
                catch
                {
                    error = true;
                }
            }

            // Second, recursively go into all sub directories
            foreach (var subDir in dir.GetDirectories()) HideFolders(subDir.FullName, error);

            // Third, fix all hidden files in the current folder
            foreach (var file in dir.GetFiles())
            {
                if ((file.Attributes & System.IO.FileAttributes.Hidden) == 0)
                {
                    try
                    {
                        File.SetAttributes(file.FullName, File.GetAttributes(file.FullName) | System.IO.FileAttributes.Hidden);
                    }
                    catch
                    {
                        error = true;
                    }
                }
            }

            return error;
        }

        public static string GetDataDir(string installDir)
        {
            string drive = Directory.GetDirectoryRoot(installDir);
            return StdPath.Combine(drive, "ProgramData", "Palette");
        }

        [CustomAction]
        public static ActionResult CreateIniFile(Session session)
        {
            session.Log("Starting custom action CreateIniFile");

            //System.Diagnostics.Debugger.Launch();

            string serverName = session.CustomActionData["SERVERNAME"].ToString().Trim();
            int port = 22;

            char[] separator = { ':' };
            string[] tokens = serverName.Split(separator, 2);
            if (tokens.Length == 2)
            {
                serverName = tokens[0];
                port = Convert.ToInt32(tokens[1]);
            }

            string installDir = session.CustomActionData["INSTALLLOCATION"].ToString();
            string path = installDir;

            string dataDir = GetDataDir(installDir);
            string logLocation = StdPath.Combine(dataDir, "logs", "agent.log");

            string licenseKey = session.CustomActionData["LICENSEKEY"].ToString();

            string tableauPath = GetTableauPath(installDir);
            if (tableauPath != null)
                path += Path.PathSeparator.ToString() + Path.Combine(tableauPath, "bin");

            try
            {
                string output = "[DEFAULT]" + Environment.NewLine;
                output += "license-key=" + licenseKey + Environment.NewLine;
                output += "uuid=" + GetPaletteUUID(installDir) + Environment.NewLine;
                output += "install-dir=" + installDir + Environment.NewLine;
                output += "data-dir=" + dataDir + Environment.NewLine;
                output += "path=" + path + Environment.NewLine;
                output += Environment.NewLine;
                output += "[controller]" + Environment.NewLine;
                output += "host=" + serverName + Environment.NewLine;
                output += "port=" + port.ToString() + Environment.NewLine;
                output += "ssl=true" + Environment.NewLine;
                output += Environment.NewLine;
                output += "[logger]" + Environment.NewLine;
                output += "location=" + logLocation + Environment.NewLine;
                output += "maxsize=10MB" + Environment.NewLine;

                string inipath = StdPath.Combine(installDir, "conf", "agent.ini");
                File.WriteAllText(inipath, Convert.ToString(output));

                ////Write the Controller username and password to a file
                //string credentialsPath = Path.Combine(installDir, @"conf\_passwd");
                //string controllerCredentials = session.CustomActionData["CONTROLLERNAME"].ToString() + "," 
                //    + session.CustomActionData["CONTROLLERPASSWORD"].ToString();

                //string hashedCreds = SHA1Util.SHA1HashStringForUTF8String(controllerCredentials);
                //File.WriteAllText(credentialsPath, hashedCreds);
            }
            catch (Exception ex)  //catch all exceptions
            {
                session.Log("Custom Action Exception: " + ex.ToString());
                return ActionResult.Failure;
            }

            session.Log("Successfully finished custom action CreateIniFile");
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult CreateEnvConf(Session session)
        {
            session.Log("Starting custom action CreateEnvConf");

            //System.Diagnostics.Debugger.Launch();

            string installDir = session.CustomActionData["INSTALLLOCATION"].ToString();

            while (installDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                installDir = installDir.Substring(0, installDir.Length - 1);
            }

            string dataDir = GetDataDir(installDir);

            try
            {
                string output = "";
                output += "Define INSTALLDIR \"" + installDir + "\"" + Environment.NewLine;
                output += "Define DATADIR \"" + dataDir + "\"" + Environment.NewLine;

                string path = StdPath.Combine(installDir, "apache2", "conf", "env.conf");
                File.WriteAllText(path, Convert.ToString(output));
            }
            catch (Exception ex)  //catch all exceptions
            {
                session.Log("Custom Action Exception: " + ex.ToString());
                return ActionResult.Failure;
            }

            session.Log("Successfully finished custom action CreateEnvConf");
            return ActionResult.Success;
        }


        [CustomAction]
        public static ActionResult CleanupAfterUninstall(Session session)
        {
            session.Log("Starting custom action CleanupAfterUninstall");
            //System.Diagnostics.Debugger.Launch();

            int result = DeleteUser("Palette", session);

            if (result == 0)
            {
                session.Log("Successfully finished custom action CleanupAfterUninstall");                
            }
            else
            {
                session.Log("CleanupAfterUninstall failed to remove Palette user");                
            }

            return ActionResult.Success;
        }

        private static int rmdir(string path, Session session)
        {
            Process process = new Process();

            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/C \"rmdir /S /Q " + path + "\"";
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardError = false;

            process.Start();
            process.WaitForExit();
            return process.ExitCode;
        }


        /// <summary>
        /// Delete the user from the system
        /// </summary>
        /// <param name="userName">the user's name</param>
        private static int DeleteUser(string userName, Session session)
        {
            string drive = Directory.GetDirectoryRoot(ProgramFilesx86());
            string userDir = StdPath.Combine(drive, "Users", userName);

            //Now remove Palette User.  First try normal means
            DirectoryEntry localDirectory = new DirectoryEntry("WinNT://" + Environment.MachineName.ToString());
            DirectoryEntries users = localDirectory.Children;

            session.Log("Attempting to delete user if it exists... ");

            try
            {
                DirectoryEntry user = users.Find(userName);
                try
                {
                    users.Remove(user);
                }
                catch (UnauthorizedAccessException ex)
                {
                    session.Log("Not authorized to remove user: " + ex.ToString());
                }
                catch (Exception ex)
                {
                    session.Log("Exception in removing user: " + ex.ToString());
                }
            }
            catch (System.Runtime.InteropServices.COMException)  //User not found.  Try to delete user folder if it exists and quit
            {
                session.Log("User not found");
            }

            int i, tries = 10;
            for (i = 0; i < tries; i++)
            {
                if (Directory.Exists(userDir))
                {
                    try
                    {
                        rmdir(userDir, session);
                        //DeleteFileSystemInfo(new DirectoryInfo(userDir), session);
                    }
                    catch (Exception e)
                    {
                        session.Log(e.ToString());
                    }
                }
                else
                {
                    break;
                }

                if (Directory.Exists(userDir))
                {
                    if (i < 10) Thread.Sleep(1000);
                }
                else
                {
                    break;
                }
            }

            if (Directory.Exists(userDir))
            {
                session.Log("Failed to delete Palette User Folder: {0} tries", tries);
                return -1;
            }
            else
            {
                session.Log("Deleted Palette User folder : {0} of {1} tries", i, tries);
            }
            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileSystemInfo"></param>
        private static int DeleteFileSystemInfo(FileSystemInfo fileSystemInfo, Session session)
        {
            int returnCode = 0;

            try
            {
                fileSystemInfo.Attributes = System.IO.FileAttributes.Normal;
            }
            catch (Exception e)
            {
                session.Log(e.ToString());
                return 1;
            }
            
            var directoryInfo = fileSystemInfo as DirectoryInfo;
            if (directoryInfo != null)
            {
                try
                {
                    foreach (var childInfo in directoryInfo.GetFileSystemInfos())
                    {
                        returnCode = DeleteFileSystemInfo(childInfo, session);
                    }
                }
                catch (Exception e)
                {
                    session.Log(e.ToString());
                    return 1;
                }
            }

            try
            {     
                fileSystemInfo.Delete();
            } catch (Exception e) {
                session.Log(e.ToString());
                returnCode++;
            }
            return returnCode;
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
        /// Run InstallerHelper.exe with the given arguments.
        /// </summary>
        /// <param name="args"></param>
        /// <returns>Standard Ouput of the command as a trimmed string or null</returns>
        private static string InstallerHelper(string binDir, string args)
        {
            //System.Diagnostics.Debugger.Launch();
            string path = binDir + @"\InstallerHelper.exe";

            Process process = new Process();

            process.StartInfo.FileName = path;
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;

            process.Start();

            string stdOut = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            string value = stdOut.Trim();
            if (value.Length == 0)
            {
                return null;
            }
            return value;
        }

        /// <summary>
        /// Finds out if Tableau is installed by querying the registry.
        /// </summary>
        /// <returns>the install path or null if not found.</returns>
        public static string GetTableauPath(string binDir)
        {
            return InstallerHelper(binDir, "tableau-install-path");
        }

        /// <summary>
        /// Get or Create a UUID and note its value in the registry.
        /// </summary>
        /// <returns>the UUID or null if not found.</returns>
        public static string GetPaletteUUID(string binDir)
        {
            return InstallerHelper(binDir, "uuid");
        }

        public static string CreatePassword(int length)
        {
            if (length < 3) throw new SystemException("Password must be longer than 3 chars");
            int subLength = length - 3;
            string valid0 = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            string valid1 = "abcdefghijklmnopqrstuvwxyz";
            string valid2 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string valid3 = "1234567890";
            string valid4 = "~!@#$%";
            string res = "";
            Random rnd = new Random();

            while (0 < subLength--)
                res += valid0[rnd.Next(valid0.Length)];

            res += valid1[rnd.Next(valid1.Length)];
            res += valid2[rnd.Next(valid2.Length)];
            res += valid3[rnd.Next(valid3.Length)];
            res += valid4[rnd.Next(valid4.Length)];
            res += valid4[rnd.Next(valid4.Length)];
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
