using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LSA;

namespace lsaadd
{
    class Program
    {
        static int Main(string[] args)
        {
            string account;
            string right;

            if (args.Length == 1)
            {
                account = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                right = args[0];
            }
            else if (args.Length == 2)
            {
                account = args[0];
                right = args[1];
            }
            else
            {
                Console.Error.WriteLine("usage: lsaadd [username] [right]\n");
                return -1;
            }

            string[] rights;
            using (LsaWrapper lsa = new LsaWrapper())
            {
                try
                {
                    rights = lsa.GetRights(account);
                }
                catch (NotFoundException)
                {
                    Console.Error.WriteLine("Account '{0}' Not Found.", account);
                    return 1;
                }

                if (rights != null && rights.Contains(right))
                {
                    Console.Error.WriteLine("Account '{0}' already has the '{1}' right.", account, right);
                    return 2;
                }

                lsa.AddRight(account, right);
            }

            return 0;
        }
    }
}
