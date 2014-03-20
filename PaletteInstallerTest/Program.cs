using System;
using System.IO;
using System.Reflection;

namespace PaletteInstallerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string binDir = dir + @"\..\..\..\InstallerHelper\bin\Debug";
            string path = PaletteInstallerCA.CustomActions.GetTableauPath(binDir);
            Console.WriteLine("Tableau Registry Key: " + path);
        }
    }
}
