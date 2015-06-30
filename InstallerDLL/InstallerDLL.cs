using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Diagnostics;
using System.DirectoryServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using InstallShield.Interop;
using Microsoft.Win32;

using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices.AccountManagement;

using LSA;

public class InstallerDLL
{
    public const string DEFAULT_USERNAME = "Palette";

    public const string SERVICE_RIGHT = "SeServiceLogonRight";

    public const int ADMIN_TYPE_CREATE_NEW = 1;
    public const int ADMIN_TYPE_USE_EXISTING = 2;

    public const int ADS_UF_DONT_EXPIRE_PASSWD = 0x10000;

    public const string PRODUCT_TYPE_LANMANNT = "LanmanNT";

    public const string REG_KEYNAME = @"Software\Palette";
    public const int REG_HIDE_USER = 0x10000;

    public const string TITLE = "Palette Agent Installer";

    public const string KB_READONLY = "http://onlinehelp.tableau.com/current/server/en-us/help.htm#adminview_postgres_access.htm";
    public const string KB_SYSINFO = "http://onlinehelp.tableau.com/current/server/en-us/help.htm#service_remote.htm";

    public const int PORT = 443; /* FIXME: define this in one place */

    private static RegistryKey openRegistryKey()
    {
        RegistryKey hklm = null, key = null;
        try
        {
            hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            key = hklm.OpenSubKey(REG_KEYNAME);
            if (key == null)
            {
                throw new Exception("Failed to open Palette registry key");
            }
            return key;
        }
        finally
        {
            if (hklm != null)
            {
                hklm.Close();
            }
        }
    }

    private static int CheckTableau(RegistryKey key, ref int enableSysInfo, ref int enableReadonlyUser, ref int restartTableau)
    {
        //System.Diagnostics.Debugger.Launch();
        Tableau tabinfo = Tableau.query();
        if (tabinfo == null)
        {
            // non-primary
            return 1;
        }

        if (tabinfo.Path == null)
        {
            string msg = "The registry contains values pertaining to Tableau that Palette cannot understand.  Please contact support.";
            throw new Exception(msg);
        }

        Version minVersion = new Version(Tableau.MINIMUM_SUPPORTED_VERSION);
        if (tabinfo.Version != null)
        {
            if (tabinfo.Version < minVersion)
            {
                string msg = "The minimum supported Tableau Server version is " + Tableau.MINIMUM_SUPPORTED_VERSION;
                throw new Exception(msg);
            }
        }
        else
        {
            string msg = "Could not determine the Tableau Server version.  Please ensure that you have at least version " + Tableau.MINIMUM_SUPPORTED_VERSION;
            TopMostMessageBox.Show(msg, TITLE, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        Dictionary<string, string> settings = tabinfo.getSettings();
        if (!Tableau.readOnlyEnabled(settings))
        {
            enableReadonlyUser = 1;
            restartTableau = 1;
        }

        string[] sysInfoIPs = Tableau.allowedSysInfoIPs(settings);
        if (sysInfoIPs == null || !sysInfoIPs.Contains("127.0.0.1"))
        {
            enableSysInfo = 1;
            restartTableau = 1;
        }

        return 1;
    }



    public static int CheckTableau(ref int enableSysInfo, ref int enableReadonlyUser, ref int restartTableau)
    {

        RegistryKey key = null;
        try
        {
            key = openRegistryKey();
        }
        catch (Exception e)
        {
            TopMostMessageBox.Show(e.Message, TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw e;
        }

        try
        {
            return CheckTableau(key, ref enableSysInfo, ref enableReadonlyUser, ref restartTableau);
        }
        catch (Exception e)
        {
            TopMostMessageBox.Show(e.Message, TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw e;
        }
        finally
        {
            key.Close();
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="handle"></param>
    public static void CreateAdminUser(Int32 handle)
    {
        //System.Diagnostics.Debugger.Launch();
        string data = Msi.CustomActionHandle(handle).GetProperty("CustomActionData");

        if (data.Length == 0)
        {
            // HACK - this means we were not supposed to run in the first place
            // i.e. AdminType == 1
            log(handle, "[CreateAdminUser] nothing to do.");
            return;
        }

        string[] tokens = data.Split(";".ToCharArray(), 2);

        string account = tokens[0];
        string password = tokens[1];

        // CheckCreateUser is responsible for ensuring that the account is valid.
        string userName;
        string domainName;
        AdminUtil.ParseAccount(account, out userName, out domainName);
        
        DirectoryEntry AD = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer");
        DirectoryEntry NewUser = AD.Children.Add(userName, "user");
        NewUser.Invoke("SetPassword", new object[] { password });
        NewUser.Invoke("Put", new object[] { "Description", "Palette User for Agent Service" });

        try
        {
            NewUser.CommitChanges();
        }
        catch (Exception e)
        {
            string msg = String.Format("Failed to create user '{0}': {1}", account, e.Message);
            TopMostMessageBox.Show(msg, TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw e;
        }

        // Setting the Don't Expire flag may be prevented by system policy, so do it in a separate transaction
        // and warn, but don't abort, on failure.
        // http://wiert.me/2009/10/11/c-net-setting-or-clearing-the-password-never-expires-flag-for-a-user/
        int userFlags = ADS_UF_DONT_EXPIRE_PASSWD;
        NewUser.Properties["userFlags"].Value = userFlags;

        try
        {
            NewUser.CommitChanges();
        }
        catch (Exception e)
        {
            string msg = String.Format("Failed set the 'Don't Expire' flag, continuing.\n{0}", e.Message);
            TopMostMessageBox.Show(msg, TITLE, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        log(handle, String.Format("[CreateAdminUser] Successfully created user: {0}", account));

        DirectoryEntry grp;
        grp = AD.Children.Find("Administrators", "group");
        if (grp == null)
        {
            string msg = "'Administrators' group not found.";
            TopMostMessageBox.Show(msg, TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw new Exception(msg);
        }
        grp.Invoke("Add", new object[] { NewUser.Path.ToString() });
        log(handle, String.Format("[CreateAdminUser] Successfully added user '{0}' to group '{1}'", account, grp.ToString()));

        string productType = AdminUtil.getProductType();

        try
        {
            if (IsDomainController(productType))
            {
                // have to explicity use the full account on the domain controller...
                GrantLogonAsServiceRight(account);
            }
            else
            {
                // ... but can't use the account on a member of the domain, it fails.
                GrantLogonAsServiceRight(Environment.MachineName + "\\" + userName);
            }
            log(handle, String.Format("[CreateAdminUser] Successfully granted 'SeServiceLogonRight' to '{0}'", account));
        }
        catch (Exception e)
        {
            string msg = String.Format("Failed to grant 'SeServiceLoginRight' to '{0}'", account);
            TopMostMessageBox.Show(msg, TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw e;
        }

        try
        {
            HideUser(account);
        }
        catch (Exception e)
        {
            string msg = String.Format("Failed to hide account '{0}', continuing.\n{1}", account, e.Message);
            TopMostMessageBox.Show(msg, TITLE, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static void EnableSysInfo()
    {
        Tableau tabinfo = Tableau.query();
        Dictionary<string, string> settings = tabinfo.getSettings();
        string[] ips = Tableau.allowedSysInfoIPs(settings);
        tabinfo.enableSysInfo(ips);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="handle"></param>
    public static void EnableSysInfo(Int32 handle)
    {
        try
        {
            EnableSysInfo();
        }
        catch (Exception e)
        {
            TopMostMessageBox.Show(e.Message, TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw e;
        }
    }

    private static void EnableReadonlyUser()
    {
        Tableau tabinfo = Tableau.query();
        Dictionary<string, string> settings = tabinfo.getSettings();

        string password = GeneratePassword();
        tabinfo.enableReadonlyUser(password);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="handle"></param>
    public static void EnableReadonlyUser(Int32 handle)
    {
        try
        {
            EnableReadonlyUser();
        }
        catch (Exception e)
        {
            TopMostMessageBox.Show(e.Message, TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw e;
        }
    }

    private static void RestartTableau()
    {
        Tableau tabinfo = Tableau.query();
        tabinfo.restart();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="handle"></param>
    public static void RestartTableau(Int32 handle)
    {
        try
        {
            RestartTableau();
        }
        catch (Exception e)
        {
            TopMostMessageBox.Show(e.Message, TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw e;
        }
    }


    /// <summary>
    /// The user home folder is created at first logon.  In this case, that happens when the Service is started.
    /// Hence, this CustomAction must be separate from CreateAdminUser and be scheduled after StartService.
    /// </summary>
    /// <param name="handle"></param>
    public static void HideHomeFolder(Int32 handle)
    {
        //System.Diagnostics.Debugger.Launch();
        string data = Msi.CustomActionHandle(handle).GetProperty("CustomActionData");
        if (data.Length == 0)
        {
            // HACK - this means we were not supposed to run in the first place
            // i.e. AdminType == 1
            log(handle, "[HideHomeFolder] nothing to do.");
            return;
        }

        string account = data;

        string userName;
        string domainName;
        AdminUtil.ParseAccount(account, out userName, out domainName);

        string homeFolder = HomeFolder(userName);
        // HideTopLevelFolder
        // First, set hidden flag on the current directory (if needed)
        DirectoryInfo dir = new DirectoryInfo(homeFolder);
        if ((dir.Attributes & System.IO.FileAttributes.Hidden) == 0)
        {

            File.SetAttributes(dir.FullName, File.GetAttributes(dir.FullName) | System.IO.FileAttributes.Hidden);
            log(handle, String.Format("[HideHomeFolder] Home folder was successfully hidden: {0}", dir.FullName));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="adminType"></param>
    /// <param name="account"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public static int CheckAdminUser(int adminType, ref string account, ref string password)
    {
        //System.Diagnostics.Debugger.Launch();
        if (adminType == ADMIN_TYPE_CREATE_NEW)
        {
            // set account to pre-populates the CreateAdminUser dialog.
            account = DEFAULT_USERNAME;
        }
        else if (adminType != ADMIN_TYPE_USE_EXISTING)
        {
            return 0;
        }
        return 1;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="account"></param>
    /// <param name="password"></param>
    /// <param name="confirmPassword"></param>
    /// <returns></returns>
    public static int CheckCreateUser(ref string account, string password, string confirmPassword)
    {
        try
        {
            if (account == null || account.Length == 0)
            {
                TopMostMessageBox.Show("'User Account' is required.", TITLE, MessageBoxIcon.Error);
                return 0;
            }

            string userName;
            string domainName;
            AdminUtil.ParseAccount(account, out userName, out domainName);

            string productType = AdminUtil.getProductType();
            if (!IsDomainController(productType))
            {
                // Same as IsLocalAccount
                if (domainName != null)
                {
                    TopMostMessageBox.Show("Domain accounts may not be created in this fashion.", TITLE, MessageBoxIcon.Error);
                    return 0;
                }
            }
            else
            {
                if (!account.Contains('\\'))
                {
                    account = GetDomainName() + @"\" + account;
                }
            }

            using (LsaWrapper lsa = new LsaWrapper())
            {
                if (lsa.UserExists(userName))
                {
                    TopMostMessageBox.Show(String.Format("Account '{0}' already exists.", account), TITLE, MessageBoxIcon.Error);
                    return 0;
                }
            }

            string homeFolder = HomeFolder(userName);
            if (Directory.Exists(homeFolder))
            {
                string msg = String.Format("The home folder '{0}' already exists.", homeFolder);
                TopMostMessageBox.Show(msg + "\nYou must remove this folder before continuing.", TITLE, MessageBoxIcon.Error);
                return 0;
            }

            if (password == null || password.Length == 0)
            {
                TopMostMessageBox.Show("'Password' is required.", TITLE, MessageBoxIcon.Error);
                return 0;
            }
            if (confirmPassword == null || confirmPassword.Length == 0)
            {
                TopMostMessageBox.Show("Please confirm the password.", TITLE, MessageBoxIcon.Error);
                return 0;
            }

            if (password != confirmPassword)
            {
                TopMostMessageBox.Show("The entered passwords do not match.", TITLE, MessageBoxIcon.Error);
                return 0;
            }

            if (password.Length < 6)
            {
                TopMostMessageBox.Show("The password must contain at least six(6) characters.", TITLE, MessageBoxIcon.Error);
                return 0;
            }

            return 1;
        } catch (Exception e) {
            TopMostMessageBox.Show(e.Message + "\nPlease try an existing adminstrative user.", TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 0;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="account"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public static int CheckExistingUser(ref string account, string password)
    {
        //System.Diagnostics.Debugger.Launch();
        try
        {
            if (account == null || account.Length == 0)
            {
                return 0;
            }
            if (password == null || password.Length == 0)
            {
                return 0;
            }

            if (!account.Contains('\\'))
            {
                account = @".\" + account;
            }

            using (LsaWrapper lsa = new LsaWrapper())
            {
                string[] rights;
                try
                {
                    rights = lsa.GetRights(account);
                }
                catch (NotFoundException)
                {
                    string msg = String.Format("The specified account '{0}' does not exist.", account);
                    TopMostMessageBox.Show(msg, TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return 0;
                }

                if (rights == null || !rights.Contains(SERVICE_RIGHT))
                {
                    try
                    {
                        lsa.AddRight(account, SERVICE_RIGHT);
                    }
                    catch (Exception e)
                    {
                        string msg = String.Format("Failed to grant '{0}' to '{1}'\n{2}", SERVICE_RIGHT, account, e.Message);
                        TopMostMessageBox.Show(msg, TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        throw e;
                    }
                }
            }

            string userName;
            string domainName;

            try
            {
                PrincipalContext ctx = AdminUtil.getPrincipalContext(account, out userName, out domainName);

                if (!ctx.ValidateCredentials(userName, password))
                {
                    TopMostMessageBox.Show("Invalid username or password", TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return 0;
                }

                bool isAdmin = AdminUtil.IsBuiltInAdmin(ctx, userName);
                if (!isAdmin)
                {
                    string msg = String.Format("The account '{0}' does not belong to the BUILTIN administrator group.\nDo you want to continue?", account);
                    DialogResult result = TopMostMessageBox.Show(msg, TITLE, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (result == DialogResult.No)
                    {
                        return 0;
                    }
                }
            }
            catch (PrincipalServerDownException e)
            {
                DialogResult result = TopMostMessageBox.Show("The domain controller is unreachable: " + e.Message + "\nDo you want to continue?", TITLE, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.No)
                {
                    return 0;
                }
            }

        }
        catch (Exception e)
        {
            DialogResult result = TopMostMessageBox.Show(e.Message + "\nDo you want to continue?", TITLE, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.No)
            {
                return 0;
            }
        }

        return 1;
    }

    public static int ParseAndValidateHostnamePort(ref string HostnamePort, ref string Hostname, ref int Port)
    {
        HostnamePort = HostnamePort.ToLower();
        if (HostnamePort.StartsWith("http://"))
        {
            HostnamePort = HostnamePort.Substring(7);
        }
        else if (HostnamePort.StartsWith("https://"))
        {
            HostnamePort = HostnamePort.Substring(8);
        }
        if (HostnamePort.EndsWith("/"))
        {
            HostnamePort = HostnamePort.Substring(0, HostnamePort.Length - 1);
        }

        if (HostnamePort.Contains('/')) {
            TopMostMessageBox.Show("The hostname is invalid.  Please enter the correct hostname of your Palette server.", TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 0;
        }

        string [] tokens = HostnamePort.Split(":".ToCharArray(), 2);
        if (tokens.Length == 2) {
            Hostname = tokens[0];
            try {
                Port = Convert.ToInt16(tokens[1]);
            } catch (Exception) {
                TopMostMessageBox.Show("The specified port valid is invalid.  Please enter the correct hostname:port of your Palette server.", TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0;
            }
        } else {
            Hostname = tokens[0];
            Port = PORT;
        }

        return ValidateHostnamePort(Hostname, Port);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static string GeneratePassword()
    {
        PasswordGenerator generator = new PasswordGenerator(12, 12);
        return generator.Generate();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static bool CheckLicenseKey(string key)
    {
        if (key.Length != 36)
        {
            return false;
        }
        if ((key[8] != '-') || (key[13] != '-') || (key[18] != '-') || (key[23] != '-'))
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static int ValidateLicenseKey(string key)
    {
        //System.Diagnostics.Debugger.Launch();
        if (!CheckLicenseKey(key))
        {
            TopMostMessageBox.Show("Invalid license key format\nThe license key should have the format: 'XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX'", TITLE, MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
            return 0;
        }
        // FIXME: check licensing
        return 1;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="hostname"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    public static int ValidateHostnamePort(string hostname, int port)
    {
        //string msg = string.Format("Hostname: {0}\nPort: {1}", hostname, port);
        //MessageBox.Show(msg, "", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return 1;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="handle"></param>
    /// <returns></returns>
    public static string GenerateUUID()
    {
        return System.Guid.NewGuid().ToString();
    }

    /// <summary>
    /// ntrights +r SeServiceLogonRight -u [user]
    /// </summary>
    /// <param name="account"></param>
    private static void GrantLogonAsServiceRight(string account)
    {
        using (LsaWrapper lsa = new LsaWrapper())
        {
            lsa.AddPrivileges(account, "SeServiceLogonRight");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="account"></param>
    /// <returns></returns>
    private static string HomeFolder(string userName)
    {
        string path = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)).FullName;
        return Path.Combine(path, userName);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="folder"></param>
    /// <returns></returns>
    private static bool DeleteFolder(string folder)
    {
        int i, tries = 10;
        for (i = 0; i < tries; i++)
        {
            if (Directory.Exists(folder))
            {
                try
                {
                    Process process = new Process();

                    process.StartInfo.FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe");
                    process.StartInfo.Arguments = "/C \"rmdir /S /Q " + folder + "\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardError = false;

                    process.Start();
                    process.WaitForExit();
                }
                catch (Exception) { }
            }
            else
            {
                break;
            }

            if (Directory.Exists(folder))
            {
                if (i < 10) Thread.Sleep(1000);
            }
            else
            {
                break;
            }
        }

        return !Directory.Exists(folder);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="userName"></param>
    /// <returns></returns>
    public static void HideUser(string userName)
    {
        RegistryKey hklm;

        if (Environment.Is64BitOperatingSystem)
        {
            hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        }
        else
        {
            hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
        }

        //HKEY_LOCAL_MACHINE\Software\Microsoft\Windows NT\CurrentVersion\Winlogon\SpecialAccounts\UserList
        string key = @"Software\Microsoft\Windows NT\CurrentVersion\Winlogon\SpecialAccounts";

        RegistryKey specialAccountsSubKey = Registry.LocalMachine.OpenSubKey(key, true);
        if (specialAccountsSubKey == null)
        {
            specialAccountsSubKey = hklm.CreateSubKey(key);
        }

        RegistryKey userListSubKey = specialAccountsSubKey.OpenSubKey("UserList", true);
        if (userListSubKey == null)
        {
            userListSubKey = specialAccountsSubKey.CreateSubKey("UserList");
        }

        userListSubKey.SetValue(userName, REG_HIDE_USER, RegistryValueKind.DWord);
        userListSubKey.Close();
        specialAccountsSubKey.Close();
        hklm.Close();
    }

    public static int FinalizeCreateUser(string UsernameNew, string PasswordNew, ref string account, ref string password)
    {
        string productType = AdminUtil.getProductType();

        account = UsernameNew;
        if (!UsernameNew.Contains(@"\")) {
            if (IsDomainController(productType)) {
                account = GetDomainName() + @"\" + account;
            } else {
                account = @".\" + account;
            }
        }

        password = (string)PasswordNew.Clone();
        return 1;
    }

    private static void log(Int32 handle, string msg)
    {
        using (Msi.Install msi = Msi.CustomActionHandle(handle))
        {
            using (Msi.Record record = new Msi.Record(msg.Length + 1))
            {
                record.SetString(0, msg);
                msi.ProcessMessage(Msi.InstallMessage.Info, record);
            }
        }
    }

    private static bool IsDomainController(string productType)
    {
        return (productType == PRODUCT_TYPE_LANMANNT);
    }

    private static string GetDomainName()
    {
        string name = null;
        try
        {
            Domain domain = Domain.GetComputerDomain();
            if (domain == null) {
                throw new Exception("No computer domain found.");
            }
            name = domain.Name;
        }
        catch (Exception exc)
        {
            TopMostMessageBox.Show(exc.Message, TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw exc;
        }
        return name;
    }

}

