using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.Security.Principal;

// http://ayende.com/blog/158401/are-you-an-administrator
class AdminUtil
{
    public static bool ValidateCredentials(string username, string password)
    {
        if (username.StartsWith(@".\"))
        {
            username = username.Substring(2);
        }        

        PrincipalContext ctx;
        try
        {
            Domain.GetComputerDomain();
            try
            {
                ctx = new PrincipalContext(ContextType.Domain);
            }
            catch (PrincipalServerDownException)
            {
                // can't access domain, check local machine instead 
                ctx = new PrincipalContext(ContextType.Machine);
            }
        }
        catch (ActiveDirectoryObjectNotFoundException)
        {
            // not in a domain
            ctx = new PrincipalContext(ContextType.Machine);
        }

        return ctx.ValidateCredentials(username, password);
    }

    public static bool IsAdministratorNoCache(string username)
    {
        PrincipalContext ctx;
        try
        {
            Domain.GetComputerDomain();
            try
            {
                ctx = new PrincipalContext(ContextType.Domain);
            }
            catch (PrincipalServerDownException)
            {
                // can't access domain, check local machine instead 
                ctx = new PrincipalContext(ContextType.Machine);
            }
        }
        catch (ActiveDirectoryObjectNotFoundException)
        {
            // not in a domain
            ctx = new PrincipalContext(ContextType.Machine);
        }

        // This call is slow, but case insensitive...
        //UserPrincipal up = UserPrincipal.FindByIdentity(ctx, username);

        // http://stackoverflow.com/questions/7533790/findbyidentity-performance-differences
        UserPrincipal up = new UserPrincipal(ctx);
        up.SamAccountName = username;
        PrincipalSearcher searcher = new PrincipalSearcher(up);
        up = searcher.FindOne() as UserPrincipal;

        if (up != null)
        {
            PrincipalSearchResult<Principal> authGroups = up.GetAuthorizationGroups();
            return authGroups.Any(principal =>
                                  principal.Sid.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid) ||
                                  principal.Sid.IsWellKnown(WellKnownSidType.AccountDomainAdminsSid) ||
                                  principal.Sid.IsWellKnown(WellKnownSidType.AccountAdministratorSid) ||
                                  principal.Sid.IsWellKnown(WellKnownSidType.AccountEnterpriseAdminsSid));
        }
        return false;
    }
}
