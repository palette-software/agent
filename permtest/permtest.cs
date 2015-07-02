using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Security.Principal;

class permtest
{
    static int Main(string[] args)
    {

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
            Console.Error.WriteLine("usage: permtest [account]");
            return 1;
        }

        string path = @"C:\ProgramData\Tableau\Tableau Server\data";

        SecurityIdentifier sid = AdminUtil.GetSidForObject(account);
        EffectiveAccess access = new EffectiveAccess(path, sid);
        access.Evaluate();
        if (access.FullControl())
        {
            Console.WriteLine(String.Format("Account '{0}' has full control of '{1}'", account, path));
        }
        else
        {
            Console.WriteLine(String.Format("Account '{0}' does NOT full control of '{1}'", account, path));
        }
        return 0;
    }
}
