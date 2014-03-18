using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Net;
using System.Threading;


/// <summary>
/// Encapsulates a console app version of agent
/// </summary>
public class ConsoleAgent
{
    /// <summary>
    /// Starts a console app with a running agent
    /// </summary>
    /// <param name="args">Path of .ini file</param>
    /// <returns>Never returns anything, should run perpetually</returns>
    public static int Main(String[] args)
    {
        string inifile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\agent.ini";

        if (args.Length == 1)
        {
            inifile = args[0];
        }
        else if (args.Length != 0)
        {
            Console.Error.WriteLine("usage: ConsoleAgent [inifile]");
            return -1;
        }

        Agent agent = new Agent(inifile, false);
        agent.Run();

        return 0;
    }
}



