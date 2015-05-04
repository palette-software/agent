using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tabregtest
{
    class Program
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

            Console.WriteLine("Tableau Server {0} ({1}bit)", tabinfo.VersionString, tabinfo.Bitness);
            Console.WriteLine("Path '{0}'", tabinfo.Path);
            Console.WriteLine("Data '{0}'", tabinfo.DataPath);
            Console.WriteLine("Registry '{0}'", tabinfo.RegistryKeyPath);

            return 0;
        }
    }
}
