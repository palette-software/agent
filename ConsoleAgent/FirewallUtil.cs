using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.Win32;
using NetFwTypeLib;


class FirewallUtil
{
    public const UInt32 HRESULT_SERVICE_NOT_RUNNING = 0x800706D9;
    protected INetFwProfile fwProfile;

    /// <summary>
    /// Checks for open ports
    /// </summary>
    /// <returns>a list of open port numbers</returns>
    public List<int> CheckPorts()
    {
        SetProfile();

        //Look For All Open Ports
        INetFwOpenPort port;
        INetFwOpenPorts openports = fwProfile.GloballyOpenPorts;
        System.Collections.IEnumerator portEnumerate = openports.GetEnumerator();

        List<int> openPorts = new List<int>();
        while (portEnumerate.MoveNext())
        {
            port = (INetFwOpenPort)portEnumerate.Current;
            openPorts.Add(port.Port);
        }

        return openPorts;
    }

    /// <summary>
    /// Opens the firewall for the port numbers in the input list
    /// </summary>
    /// <param name="portsToOpen">list of port numbers</param>
    public void OpenFirewall(List<int> portsToOpen)
    {
        SetProfile();

        //Open Needed Ports 
        INetFwOpenPorts openports = fwProfile.GloballyOpenPorts;
        foreach (int port in portsToOpen)
        {
            INetFwOpenPort openport = (INetFwOpenPort)GetInstance("INetOpenPort");
            openport.Port = port;
            openport.Protocol = NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
            openport.Name = "Palette Service Port " + port;
            openports.Add(openport);
        }
        openports = null;
    }

    /// <summary>
    /// Closes the Firewall for the port numbers in the input list
    /// </summary>
    /// <param name="portsToClose">list of port numbers</param>
    public void CloseFirewall(List<int> portsToClose)
    {
        SetProfile();

        //Closed Un-needed Ports
        INetFwAuthorizedApplications apps = fwProfile.AuthorizedApplications;
        INetFwOpenPorts ports = fwProfile.GloballyOpenPorts;
        foreach (int port in portsToClose)
        {
            ports.Remove(port, NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP);
        }
        ports = null;
    }

    /// <summary>
    /// Sets the profile for the Firewall manager to local
    /// </summary>
    protected void SetProfile()
    {
        //Access INetFwMgr
        INetFwMgr fwMgr = (INetFwMgr)GetInstance("INetFwMgr");
        INetFwPolicy fwPolicy = fwMgr.LocalPolicy;
        fwProfile = fwPolicy.CurrentProfile;
        fwMgr = null;
        fwPolicy = null;
    }

    /// <summary>
    /// Gets an instance of either the Firewall manager, Authorization app, or OpenPort app using each one's GUID
    /// </summary>
    /// <param name="typeName">type of instance to retrieve</param>
    /// <returns>object of type Firewall manager, Authorization app, or OpenPort app</returns>
    protected Object GetInstance(String typeName)
    {
        if (typeName == "INetFwMgr")
        {
            Type type = Type.GetTypeFromCLSID(
            new Guid("{304CE942-6E39-40D8-943A-B913C40C9CD4}"));
            return Activator.CreateInstance(type);
        }
        else if (typeName == "INetAuthApp")
        {
            Type type = Type.GetTypeFromCLSID(
            new Guid("{EC9846B3-2762-4A6B-A214-6ACB603462D2}"));
            return Activator.CreateInstance(type);
        }
        else if (typeName == "INetOpenPort")
        {
            Type type = Type.GetTypeFromCLSID(
            new Guid("{0CA545C6-37AD-4A6C-BF92-9F7610067EF5}"));
            return Activator.CreateInstance(type);
        }
        else return null;
    }
}
