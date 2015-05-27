using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices.AccountManagement;

using LSA;

namespace lsatest
{
    class Program
    {
        static int Main(string[] args)
        {
            // returns the account the application is running as.
            string account;
            string password = null;
            if (args.Length == 0)
            {
                account = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            }
            else if (args.Length == 1)
            {
                account = args[0];
            }
            else if (args.Length == 2)
            {
                account = args[0];
                password = args[1];
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
                }
                else
                {

                    Console.WriteLine(account + ":");
                    for (int i = 0; i < rights.Length; i++)
                    {
                        Console.WriteLine("  " + rights[i]);
                    }
                }
            }

            string userName;
            string domainName;

            Console.WriteLine();
            try
            {
                PrincipalContext ctx = AdminUtil.getPrincipalContext(account, out userName, out domainName);

                if (domainName != null)
                {
                    Console.WriteLine("The current domain is '{0}'.", domainName);
                }

                bool isAdmin = AdminUtil.IsAdministratorNoCache(ctx, userName);
                if (isAdmin)
                {
                    Console.WriteLine("'{0}' is an administrator.", account);
                }
                else
                {
                    Console.WriteLine("'{0}' is NOT an administrator.", account);
                }

                if (password != null)
                {
                    bool isValid = ctx.ValidateCredentials(userName, password);
                    if (isValid)
                    {
                        Console.WriteLine("The credentials are valid.");
                    }
                    else
                    {
                        Console.WriteLine("The credentials are INVALID.");
                    }
                }
            }
            catch (PrincipalServerDownException e)
            {
                Console.WriteLine("The domain controller is unreachable: " + e.Message);
            }

            
            return 0;
        }
    }
}
