using System;
using System.IO;

public class Agent
{
    public static int Main(String[] args)
    {
        PaletteHandler handler = new PaletteHandler();

        HttpProcessor processor = new HttpProcessor();
        processor.Connect();
        processor.Run(handler);
        // currently never reached.
        processor.Close();

        return 0;
    }
}



