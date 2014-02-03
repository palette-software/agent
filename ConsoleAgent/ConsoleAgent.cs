using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Net;
using System.Threading;

public class ConsoleAgent
{

    public static int Main(String[] args)
    {
        string inifile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\agent.ini";

        if (args.Length == 1)
        {
            inifile = args[1];
        }
        else if (args.Length != 0)
        {
            Console.WriteLine("usage: %s [inifile]", args[0]);
            return -1;
        }

        Agent agent = new Agent(inifile);
        agent.Run();

        return 0;
    }
}



