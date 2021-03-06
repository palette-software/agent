﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Palette Console Agent")]
[assembly: AssemblyDescription("Palette Console Agent")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Palette")]
[assembly: AssemblyProduct("Palette Console Agent")]
[assembly: AssemblyCopyright("Copyright © Palette 2014")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("321caff6-a49b-49e1-874a-49fa1ea961d8")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
[assembly: AssemblyVersion("1.6.1.*")]
[assembly: log4net.Config.XmlConfigurator(Watch = true)]

// Setting internals visible to the unit test project
[assembly: InternalsVisibleTo("ConsoleAgentTests")]
