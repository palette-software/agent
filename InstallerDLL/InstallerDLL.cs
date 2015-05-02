using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using InstallShield.Interop;

namespace InstallerDLL
{
    public class InstallerDLL
    {
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

        public static int ValidateHostnamePort(string hostname, int port)
        {
            //string msg = string.Format("Hostname: {0}\nPort: {1}", hostname, port);
            //MessageBox.Show(msg, "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return 1;
        }

        public static string GenerateUUID(Int32 handle)
        {
            return System.Guid.NewGuid().ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="num"></param>
        public static void PrintValue(Int32 num)
        {
            string value = Convert.ToString(num);
            MessageBox.Show(value, "Print Value", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
        }

        /// <summary>
        /// EchoCustomActionData retrieves the CustomActionData property and shows it in a messagebox; then aborts the setup
        /// </summary>
        public static void EchoCustomActionData(Int32 handle)
        {
            string data = Msi.CustomActionHandle(handle).GetProperty("CustomActionData");
            MessageBox.Show(data, "EchoCustomActionData", MessageBoxButtons.OK, MessageBoxIcon.Information);
            throw new Exception("Aborting setup by throwing an exception");
        }
    }
}
