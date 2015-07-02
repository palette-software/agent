using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using InstallShield.Interop;

internal class DeferredCustomAction
{
    protected Int32 handle;
    private string[] tokens = null;
    private string title = null;

    public DeferredCustomAction(Int32 handle, string title)
    {
        this.handle = handle;
        this.title = title;
        getProperties();
    }

    public DeferredCustomAction(Int32 handle) : this(handle, null) {}

    private void getProperties()
    {
        string data = Msi.CustomActionHandle(handle).GetProperty("CustomActionData");
        if (data.Length == 0)
        {
            return;
        }
        tokens = data.Split(";".ToCharArray());
    }

    public int Length
    {
        get
        {
            if (tokens == null)
            {
                return 0;
            }
            return tokens.Length;
        }
    }

    public string this[int index]
    {
        get
        {
            return tokens[index];
        }
    }

    private void ProcessMessage(string msg, Msi.InstallMessage level)
    {
        if (title != null)
        {
            msg = "[" + title + "] " + msg;
        }

        using (Msi.Install msi = Msi.CustomActionHandle(handle))
        {
            using (Msi.Record record = new Msi.Record(msg.Length + 1))
            {
                record.SetString(0, msg);
                msi.ProcessMessage(level, record);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="handle"></param>
    /// <param name="msg"></param>
    public void log(string msg)
    {
        ProcessMessage(msg, Msi.InstallMessage.Info);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="msg"></param>
    public void error(string msg)
    {
        ProcessMessage(msg, Msi.InstallMessage.Error);
    }
}
