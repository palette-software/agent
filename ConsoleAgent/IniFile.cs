using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// Encapsulates an .ini file for agent configuration settings
/// </summary>
public class IniFile
{
    string Path;
    string EXE = Assembly.GetExecutingAssembly().GetName().Name;

    [DllImport("kernel32")]
    static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

    [DllImport("kernel32")]
    static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="IniPath">Path of .ini file</param>
    public IniFile(string IniPath)
    {
        Path = new FileInfo(IniPath ?? EXE + ".ini").FullName.ToString();
    }

    public IniFile() : this(null) { }

    /// <summary>
    /// Reads a Key and Section of .ini file and returns a profile string
    /// </summary>
    /// <param name="Key">Key string</param>
    /// <param name="Section">Section String</param>
    /// <returns>Profile string</returns>
    public string Read(string Key, string Section)
    {
        var RetVal = new StringBuilder(255);
        GetPrivateProfileString(Section ?? EXE, Key, "", RetVal, 255, Path);
        return RetVal.ToString();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Key"></param>
    /// <param name="Section"></param>
    /// <returns></returns>
    public int ReadInt(string Key, string Section)
    {
        return Convert.ToInt32(Read(Key, Section));
    }

    public int ReadInt(string Key, String Section, int Default)
    {
        if (KeyExists(Key, Section))
        {
            return ReadInt(Key, Section);
        }
        return Default;
    }

    public bool ReadBool(string Key, String Section)
    {
        return Convert.ToBoolean(Read(Key, Section));
    }

    public bool ReadBool(string Key, String Section, bool Default)
    {
        if (KeyExists(Key, Section))
        {
            return ReadBool(Key, Section);
        }
        return Default;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="Key"></param>
    /// <param name="Section"></param>
    /// <param name="Default"></param>
    /// <returns></returns>
    public string Read(string Key, string Section, string Default)
    {
        if (KeyExists(Key, Section))
        {
            return Read(Key, Section);
        }
        return Default;
    }




    /// <summary>
    /// Writes a Key, Value, Section to .ini file
    /// </summary>
    /// <param name="Key">Key string</param>
    /// <param name="Value">Value string</param>
    /// <param name="Section">Section string</param>
    public void Write(string Key, string Value, string Section)
    {
        WritePrivateProfileString(Section ?? EXE, Key, Value, Path);
    }

    /// <summary>
    /// Deletes a key in .ini file
    /// </summary>
    /// <param name="Key">Key to be deleted</param>
    /// <param name="Section">Section string</param>
    public void DeleteKey(string Key, string Section)
    {
        Write(Key, null, Section ?? EXE);
    }

    /// <summary>
    /// Deletes a Section in .ini file
    /// </summary>
    /// <param name="Section">Section to be deleted</param>
    public void DeleteSection(string Section)
    {
        Write(null, null, Section ?? EXE);
    }

    /// <summary>
    /// Checks for existence of key
    /// </summary>
    /// <param name="Key">Key string</param>
    /// <param name="Section">Section string</param>
    /// <returns>true if exists, false if not</returns>
    public bool KeyExists(string Key, string Section)
    {
        return Read(Key, Section).Length > 0;
    }
}
