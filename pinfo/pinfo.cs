using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class pinfo
{
    /// <summary>
    /// Writes a serialized JSON string to StdOut with usage information for each drive on the system
    /// </summary>
    /// <param name="args">currently only /du to return disk usage information</param>
    /// <returns>nested JSON</returns>
    /// 
    static int Main(string[] args)
    {
        try
        {
            Tableau tabinfo = Tableau.query();
            Dictionary<string, object> allData = Info.Generate(tabinfo);
            string json = fastJSON.JSON.Instance.ToJSON(allData);
            Console.WriteLine(json);
        }
        catch (Exception exc)
        {
            Console.Error.WriteLine(exc.ToString());
            return -1;
        }

        return 0;
    }
}
