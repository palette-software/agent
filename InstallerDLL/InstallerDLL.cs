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

using LSA;

public class InstallerDLL
{
    public const string USERNAME = "palette";

    public const string SERVICE_RIGHT = "SeServiceLogonRight";

    public const int ADMIN_TYPE_CREATE_NEW = 1;
    public const int ADMIN_TYPE_USE_EXISTING = 2;

    public const int ADS_UF_DONT_EXPIRE_PASSWD = 0x10000;

    public const string PRODUCT_TYPE_LANMANNT = "LanmanNT";

    /// <summary>
    /// 
    /// </summary>
    /// <param name="productType"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
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

        string username = tokens[0];
        string password = tokens[1];

        if (username.StartsWith(@".\"))
        {
            username = username.Substring(2);
        }

        DirectoryEntry AD = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer");
        DirectoryEntry NewUser = AD.Children.Add(username, "user");
        NewUser.Invoke("SetPassword", new object[] { password });
        NewUser.Invoke("Put", new object[] { "Description", "Palette User for Agent Service" });

        try
        {
            NewUser.CommitChanges();
        }
        catch (Exception e)
        {
            string msg = String.Format("Failed to create user '{0}': {1}", username, e.Message);
            MessageBox.Show(msg, "CreateAdminUser", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            MessageBox.Show(msg, "CreateAdminUser", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        log(handle, String.Format("[CreateAdminUser] Successfully created user: {0}\\{1}", Environment.MachineName, username));

        DirectoryEntry grp;
        grp = AD.Children.Find("Administrators", "group");
        if (grp == null)
        {
            string msg = "'Administrators' group not found.";
            MessageBox.Show(msg, "CreateAdminUser", MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw new Exception(msg);
        }
        grp.Invoke("Add", new object[] { NewUser.Path.ToString() });
        log(handle, String.Format("[CreateAdminUser] Successfully added user '{0}\\{1}' to group '{2}'", Environment.MachineName, username, grp.ToString()));

        try
        {
            GrantLogonAsServiceRight(Environment.MachineName + "\\" + username);
            log(handle, String.Format("[CreateAdminUser] Successfully granted 'SeServiceLogonRight' to '{0}\\{1}'", Environment.MachineName, username));
        }
        catch (Exception e)
        {
            string msg = String.Format("Failed to grant 'SeServiceLoginRight' to '{0}\\{1}'", Environment.MachineName, username);
            MessageBox.Show(msg, "CreateAdminUser", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        string username = data;
        if (username.StartsWith(@".\"))
        {
            username = username.Substring(2);
        }

        string homeFolder = HomeFolder(username);
        // HideTopLevelFolder
        // First, set hidden flag on the current directory (if needed)
        DirectoryInfo dir = new DirectoryInfo(homeFolder);
        if ((dir.Attributes & System.IO.FileAttributes.Hidden) == 0)
        {

            File.SetAttributes(dir.FullName, File.GetAttributes(dir.FullName) | System.IO.FileAttributes.Hidden);
            log(handle, String.Format("[HideHomeFolder] Home folder was successfully hidden: {0}", dir.FullName));
        }
    }

    public static void Rollback(Int32 handle)
    {
        string data = Msi.CustomActionHandle(handle).GetProperty("CustomActionData");
        log(handle, "[RollbackCustomAction] " + data);

        string[] tokens = data.Split(";".ToCharArray(), 2);

        int adminType = Convert.ToInt32(tokens[0]);
        if (adminType == ADMIN_TYPE_CREATE_NEW)
        {
            DeleteAdminUser(handle);
        }

        string installDir = tokens[1];

    }

    public static void DeleteAdminUser(Int32 handle)
    {
        string homeFolder = HomeFolder(USERNAME);

        //Now remove Palette User.  First try normal means
        DirectoryEntry localDirectory = new DirectoryEntry("WinNT://" + Environment.MachineName.ToString());
        DirectoryEntries users = localDirectory.Children;

        try
        {
            DirectoryEntry user = users.Find(USERNAME);
            try
            {
                users.Remove(user);
                log(handle, String.Format("[DeleteAdminUser] successfully deleted {0}", USERNAME));
            }
            catch (Exception e) {
                log(handle, String.Format("[DeleteAdminUser] Failed to delete user {0}: {1}", USERNAME, e.Message));
            }
        }
        catch (System.Runtime.InteropServices.COMException e)
        {
            //User not found.  Try to delete user folder if it exists and quit
            log(handle, String.Format("[DeleteAdminUser] {0}: {1}", USERNAME, e.Message));
        }

        if (Directory.Exists(homeFolder))
        {
            bool result = DeleteFolder(homeFolder);
            if (result)
            {
                log(handle, String.Format("[DeleteAdminUser] successfully deleted home folder: {0}", homeFolder));
            }
            else
            {
                log(handle, String.Format("[DeleteAdminUser] failed to delete home folder: {0}", homeFolder));
            }
        }
        else
        {
            log(handle, String.Format("[DeleteAdminUser] home folder did not exist: {0}", homeFolder));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="adminType"></param>
    /// <param name="productType"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public static int CheckAdminUser(int adminType, string productType, ref string username, ref string password)
    {
        //System.Diagnostics.Debugger.Launch();
        if (adminType == ADMIN_TYPE_CREATE_NEW)
        {
            // set username to pre-populates the CreateAdminUser dialog.
            username = @".\" + USERNAME;
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
    /// <param name="productType"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="confirmPassword"></param>
    /// <returns></returns>
    public static int CheckCreateUser(string productType, string username, string password, string confirmPassword)
    {
        if (username == null || username.Length == 0)
        {
            MessageBox.Show("'username' is required.", "Create User Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
            return 0;
        }
        if (password == null || password.Length == 0)
        {
            MessageBox.Show("'password' is required.", "Create User Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
            return 0;
        }
        if (confirmPassword == null || confirmPassword.Length == 0)
        {
            MessageBox.Show("Please confirm the password.", "Create User Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
            return 0;
        }

        if (password != confirmPassword)
        {
            MessageBox.Show("The entered passwords do not match.", "Create User Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
            return 0;
        }

        if (password.Length < 8)
        {
            MessageBox.Show("The password must contain at least at characters.", "Create User Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
            return 0;
        }

        return 1;
    }

    public static int CheckExistingUser(string productType, string username, string password)
    {
        if (username == null || username.Length == 0)
        {
            return 0;
        }
        if (password == null || password.Length == 0)
        {
            return 0;
        }
        if (!username.Contains('\\'))
        {
            username = @".\" + username;
        }

        if (productType != PRODUCT_TYPE_LANMANNT)
        {
            using (LsaWrapper lsa = new LsaWrapper())
            {
                string[] rights;
                try
                {
                    rights = lsa.GetRights(username);
                }
                catch (NotFoundException)
                {
                    string msg = String.Format("The specified account '{0}' does not exist.", username);
                    MessageBox.Show(msg, "Check User Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                    return 1;
                }

                if (rights == null || !rights.Contains(SERVICE_RIGHT))
                {
                    try
                    {
                        lsa.AddRight(username, SERVICE_RIGHT);
                    }
                    catch (Exception e)
                    {
                        string msg = String.Format("Failed to grant '{0}' to '{1}'\n{2}", SERVICE_RIGHT, username, e.Message);
                        MessageBox.Show(msg, "Check User Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                        throw e;
                    }
                }
            }
        }


        return 1;
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
            MessageBox.Show("Invalid license key format\nThe license key should have the format: 'XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX'", "License Key Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
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
    public static string GenerateUUID(Int32 handle)
    {
        return System.Guid.NewGuid().ToString();
    }

    /// <summary>
    /// ntrights +r SeServiceLogonRight -u [user]
    /// </summary>
    /// <param name="username"></param>
    private static void GrantLogonAsServiceRight(string username)
    {
        using (LsaWrapper lsa = new LsaWrapper())
        {
            lsa.AddPrivileges(username, "SeServiceLogonRight");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="username"></param>
    /// <returns></returns>
    private static string HomeFolder(string username)
    {
        string path = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)).FullName;
        return Path.Combine(path, username);
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

}

