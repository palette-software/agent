using System;
using System.Collections.Generic;
using System.Linq;
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
            try
            {
                string output = "[DEFAULT]" + Environment.NewLine;
                output += "type=primary" + Environment.NewLine;
                output += "# archive=false" + Environment.NewLine;
                output += "uuid=" + System.Guid.NewGuid().ToString() + Environment.NewLine;  
                output += "install-dir=" + session["INSTALLLOCATION"].ToString() + Environment.NewLine; 
                output += "# hostname=primary" + Environment.NewLine;
                output += Environment.NewLine;
                output += "[controller]" + Environment.NewLine;
                output += "# host=" + session["SERVERNAME"].ToString().Trim() + Environment.NewLine;
                output += "# port=8888" + Environment.NewLine;
                output += Environment.NewLine;
                output += "[archive]" + Environment.NewLine;
                output += "# listen-port=8889" + Environment.NewLine;
                output += Environment.NewLine;
                output += "[logging]" + Environment.NewLine;
                output += "location=" + session["INSTALLLOCATION"].ToString() + @"log\agent.log" + Environment.NewLine;
                output += "maxsize=10MB" + Environment.NewLine;

                string path = Path.Combine(session["INSTALLLOCATION"].ToString(), @"conf\agent.ini");
                File.WriteAllText(path, Convert.ToString(output));
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
    }
}
