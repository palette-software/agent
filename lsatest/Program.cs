using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LSA;

namespace lsatest
{
    class Program
    {
        static int Main(string[] args)
        {
            // returns the account the application is running as.
            string account;
            if (args.Length == 0)
            {
                account = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            }
            else if (args.Length == 1)
            {
                account = args[0];
            }
            else
            {
                Console.Error.WriteLine("usage: lsatest [account]");
                return -1;
            }

            Console.WriteLine();
            using (LsaWrapper lsa = new LsaWrapper())
            {
                string [] rights = lsa.GetRights(account);
                if (rights == null)
                {
                    Console.WriteLine("Account '{0}' has no granted rights.", account);
                    return 0;
                }

                Console.WriteLine(account + ":");
                for (int i = 0; i < rights.Length; i++)
                {
                    Console.WriteLine("  " + rights[i]);
                }
            }
            return -1;
        }
    }
}
