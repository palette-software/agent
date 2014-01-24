using System;
using System.IO;

public class Agent
{
    public static int Main(String[] args)
    {
        HttpProcessor processor = new HttpProcessor();
        processor.Connect();
        processor.Run();
        // currently never reached.
        processor.Close();

        return 0;
    }
}



