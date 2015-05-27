using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.Security.Principal;

using Microsoft.Win32;

// http://ayende.com/blog/158401/are-you-an-administrator
class AdminUtil
{
    public const string REG_KEY_PRODUCT_OPTIONS = @"SYSTEM\CurrentControlSet\Control\ProductOptions";

    /// <summary>
    /// PrincipalServerDownException must be caught: it means the system is in a domain, but the DC is unavailable.
    /// </summary>
    /// <param name="account"></param>
    /// <param name="userName"></param>
    /// <param name="domainName"></param>
    /// <returns></returns>
    public static PrincipalContext getPrincipalContext(string account, out string userName, out string domainName)
    {
        domainName = null;
        string[] tokens = account.Split("\\".ToCharArray(), 2);

        PrincipalContext ctx;
        if (tokens.Length == 1)
        {
            userName = tokens[0];
        } else {
            if (tokens[0] != "." && tokens[0] != Environment.MachineName)
            {
                domainName = tokens[0];
            }
            userName = tokens[1];
        }
            
        if (domainName != null)
        {
            ctx = new PrincipalContext(ContextType.Domain, domainName);
        } else {
            ctx = new PrincipalContext(ContextType.Machine);
        }
        
        return ctx;
    }

    /// <summary>
    /// Must catch PrincipalServerDownException
    /// </summary>
    /// <param name="account"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public static bool ValidateCredentials(string account, string password)
    {
        string userName;
        string domainName;

        PrincipalContext ctx = getPrincipalContext(account, out userName, out domainName);
        return ctx.ValidateCredentials(userName, password);
    }

    /// <summary>
    /// Must catch PrincipalServerDownException
    /// </summary>
    /// <param name="account"></param>
    /// <returns></returns>
    public static bool IsBuiltInAdmin(string account)
    {
        string userName;
        string domainName;

        PrincipalContext ctx = getPrincipalContext(account, out userName, out domainName);
        return IsBuiltInAdmin(ctx, userName);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="userName"></param>
    /// <returns></returns>
    public static bool IsBuiltInAdmin(PrincipalContext ctx, string userName)
    {
        // This call is slow, but case insensitive...
        UserPrincipal up = UserPrincipal.FindByIdentity(ctx, userName);

        // http://stackoverflow.com/questions/7533790/findbyidentity-performance-differences
        //UserPrincipal up = new UserPrincipal(ctx);
        //up.SamAccountName = userName;
        //PrincipalSearcher searcher = new PrincipalSearcher(up);
        //up = searcher.FindOne() as UserPrincipal;

        if (up != null)
        {
            //PrincipalSearchResult<Principal> authGroups = up.GetAuthorizationGroups();
            PrincipalSearchResult<Principal> authGroups = up.GetGroups();
            /*
            return authGroups.Any(principal =>
                                  principal.Sid.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid) ||
                                  principal.Sid.IsWellKnown(WellKnownSidType.AccountDomainAdminsSid) ||
                                  principal.Sid.IsWellKnown(WellKnownSidType.AccountAdministratorSid) ||
                                  principal.Sid.IsWellKnown(WellKnownSidType.AccountEnterpriseAdminsSid));
            */
            return authGroups.Any(principal =>
                                  principal.Sid.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid));
        }
        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static string getProductType()
    {
        RegistryKey key = Registry.LocalMachine.OpenSubKey(REG_KEY_PRODUCT_OPTIONS);
        if (key == null)
        {
            return null;
        }
        string productType = (string)key.GetValue("ProductType");
        key.Close();

        return productType;
    }
}
