using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


class tabtest
{
    static int Main(string[] args)
    {
        bool needRestart = false;

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

        if (!Tableau.readOnlyEnabled(settings))
        {
            tabinfo.enableReadonlyUser("p@ssword");
            Console.WriteLine("readonly user successfully enabled.");
            needRestart = true;
        } else {
            Console.WriteLine("readonly user was already enabled.");
        }

        // http://onlinehelp.tableau.com/current/server/en-us/help.htm#service_remote.htm
        string[] ips = Tableau.allowedSysInfoIPs(settings);
        if (ips == null || !ips.Contains("127.0.0.1"))
        {
            tabinfo.enableSysInfo(ips);
            Console.WriteLine("sysinfo enabled for localhost");
            needRestart = true;
        }
        else
        {
            Console.WriteLine("sysinfo was already enabled.");
        }

        if (needRestart)
        {
            tabinfo.restart();
            Console.WriteLine("tableau server successfully restarted.");
        }
        
        return 0;
    }
}
