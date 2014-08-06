using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

class NetUtil
{
    /// <summary>
    /// Gets the resolved ip address for a specified server name or url.  See 
    /// http://www.codeproject.com/Tips/440861/Resolving-a-hostname-in-Csharp-and-retrieving-IP-v
    /// </summary>
    /// <param name="serverNameOrURL">Server name or URL</param>
    /// <param name="resolvedIPAddress">Out: the resolved ip address</param>
    /// <returns></returns>
    public static bool GetResolvedConnectionIPAddress(string serverNameOrURL,
                   out IPAddress resolvedIPAddress)
    {
        bool isResolved = false;
        IPHostEntry hostEntry = null;
        IPAddress resolvIP = null;
        try
        {
            if (!IPAddress.TryParse(serverNameOrURL, out resolvIP))
            {
                hostEntry = Dns.GetHostEntry(serverNameOrURL);

                if (hostEntry != null && hostEntry.AddressList != null
                             && hostEntry.AddressList.Length > 0)
                {
                    if (hostEntry.AddressList.Length == 1)
                    {
                        resolvIP = hostEntry.AddressList[0];
                        isResolved = true;
                    }
                    else
                    {
                        foreach (IPAddress var in hostEntry.AddressList)
                        {
                            if (var.AddressFamily == AddressFamily.InterNetwork)
                            {
                                resolvIP = var;
                                isResolved = true;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                isResolved = true;
            }
        }
        catch
        {
            isResolved = false;
            resolvIP = null;
        }
        finally
        {
            resolvedIPAddress = resolvIP;
        }
        return isResolved;
    }
}
