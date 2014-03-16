using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.ServiceProcess;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Win32;

namespace PaletteInstallerCleanup
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult CleanupAfterInstall(Session session)
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

                //TimeSpan timeout = TimeSpan.FromSeconds(10);
                //ServiceController service = new ServiceController("Palette");
                //service.Stop();
                //service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);

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
        public static string ProgramFilesx86()
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
