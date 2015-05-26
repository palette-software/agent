using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


class tabtest
{
    static int Main(string[] args)
    {
        Console.WriteLine();

        Tableau tabinfo = Tableau.query();
        if (tabinfo == null)
        {
            Console.WriteLine("No Tableau information found.");
            return 0;
        }

        Console.WriteLine("Tableau Server {0} ({1}bit)", tabinfo.Version, tabinfo.Bitness);
        Console.WriteLine("Path '{0}'", tabinfo.Path);
        Console.WriteLine("Data '{0}'", tabinfo.DataPath);
        Console.WriteLine("Registry '{0}'", tabinfo.RegistryKeyPath);

        // http://onlinehelp.tableau.com/current/server/en-us/help.htm#adminview_postgres_access.htm
        Dictionary<string, string> settings = tabinfo.getSettings();
        Console.WriteLine("readonly user: {0}", Tableau.readOnlyEnabled(settings));

        // http://onlinehelp.tableau.com/current/server/en-us/help.htm#service_remote.htm
        string[] ips = Tableau.allowedSysInfoIPs(settings);
        Console.WriteLine("sysinfo enabled: {0}", ips.Contains("127.0.0.1"));

        return 0;
    }
}
