using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

public class IniFile
{
    string Path;
    string EXE = Assembly.GetExecutingAssembly().GetName().Name;

    [DllImport("kernel32")]
    static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

    [DllImport("kernel32")]
    static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

    public IniFile(string IniPath)
    {
        Path = new FileInfo(IniPath ?? EXE + ".ini").FullName.ToString();
    }

    public IniFile() : this(null) { }

    public string Read(string Key, string Section)
    {
        var RetVal = new StringBuilder(255);
        GetPrivateProfileString(Section ?? EXE, Key, "", RetVal, 255, Path);
        return RetVal.ToString();
    }

    public void Write(string Key, string Value, string Section)
    {
        WritePrivateProfileString(Section ?? EXE, Key, Value, Path);
    }

    public void DeleteKey(string Key, string Section)
    {
        Write(Key, null, Section ?? EXE);
    }

    public void DeleteSection(string Section)
    {
        Write(null, null, Section ?? EXE);
    }

    public bool KeyExists(string Key, string Section)
    {
        return Read(Key, Section).Length > 0;
    }
}
