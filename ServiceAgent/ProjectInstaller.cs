using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using System.Diagnostics;


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
            //Agent.SetupConfig();

            ServiceController sc = new ServiceController(serviceInstaller1.ServiceName);
            sc.Start();
        }

        //This requires "Primary output from ServiceAgent (Active)" in the uninstall action
        //If this fails and you can't install or uninstall, go to "HKEY_USERS" in Registry, 
        //and search for "ServiceAgent". Delete the folder containing a reference to it
        //in \Software\Microsoft\Installer\Products 
        public override void Uninstall(IDictionary savedState)
        {
            base.Uninstall(savedState);

            using (Process process = new Process())
            {
                process.StartInfo.FileName = @"sc.exe";
                process.StartInfo.Arguments = "delete ServiceAgent";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.Start();
            }        
        }
    }
}
