using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.IO;
using System.Reflection;


namespace ServiceAgent
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
            serviceInstaller1.Description = "Service Agent for Tableau";
            this.AfterInstall += new InstallEventHandler(ServiceInstaller_AfterInstall);
        }

        //TODO: This needs to be tested
        void ServiceInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            //Create the .ini file
            SetupConfig();

            ServiceController sc = new ServiceController(serviceInstaller1.ServiceName);
            sc.Start();
        }

        void SetupConfig()
        {
            FileInfo f = new FileInfo(Assembly.GetExecutingAssembly().Location);
            string drive = Path.GetPathRoot(f.FullName);

            string ver = Agent.GetTableauVersion();
            string iniTemplate = Path.Combine(drive, "Palette\\conf\\primary.ini");

            if (ver == null)
            {
                iniTemplate = Path.Combine(drive, "Palette\\conf\\other.ini");
                if (File.Exists(iniTemplate))
                {
                    File.Copy(iniTemplate, Path.Combine(drive, "Palette\\conf\\agent.ini"));
                }
                else  //This should never happen
                {
                    throw new SystemException("No other.ini file available");
                }
            }
            else
            {
                if (File.Exists(iniTemplate))
                {
                    File.Copy(iniTemplate, Path.Combine(drive, "Palette\\conf\\agent.ini"));
                }
                else  //This should never happen
                {
                    throw new SystemException("No primary.ini file available");
                }
            }            
        }
    }
}
