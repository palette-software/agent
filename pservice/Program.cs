using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceProcess;

class Program
{
    /// <summary>
    /// Pings a service to give status (running, stopped) or starts/stops a service
    /// </summary>
    /// <param name="args">start|stop|ping serviceName</param>
    /// <returns>-1 for exception, 0 otherwise</returns>
    static int Main(string[] args)
    {
        string serviceName = "";
        bool startService = false;
        bool stopService = false;
        bool pingService = false;

        if (args.Length < 2)
        {
            //Example: pservice start|stop|ping serviceName
            //Example: pservice stop ServiceAgent
            Console.WriteLine("Usage: pservice start|stop|ping serviceName");
            return -1;
        }
        else
        {
            switch (args[0])
            {
                case "start":
                    startService = true;
                    break;
                case "stop":
                    stopService = true;
                    break;
                case "ping":
                    pingService = true;
                    break;
                default:
                    Console.WriteLine("Usage: pservice start|stop|ping serviceName");
                    return -1;
            }

            serviceName = args[1];
        }

        ServiceController sc;

        try
        {
            sc = new ServiceController(serviceName);

            if (pingService)
            {
                Console.WriteLine("The service status is currently {0}", sc.Status.ToString());
                return 0;
            }

            if (startService)
            {
                if ((sc.Status.Equals(ServiceControllerStatus.Stopped)) ||
                     (sc.Status.Equals(ServiceControllerStatus.StopPending)))
                {
                    // Start the service if the current status is stopped.
                    Console.WriteLine("Starting the service...");
                    sc.Start();
                }
                else
                {
                    Console.WriteLine("The service is already running");
                }
            }

            if (stopService)
            {
                if ((sc.Status.Equals(ServiceControllerStatus.Running)) ||
                     (sc.Status.Equals(ServiceControllerStatus.StartPending)))
                {
                    // Stop the service if its status is not set to "Stopped".
                    Console.WriteLine("Stopping the service...");
                    sc.Stop();
                }
                else
                {
                    Console.WriteLine("The service is already stopped");
                }
            }

            // Refresh and display the current service status.
            sc.Refresh();
            Console.WriteLine("The service status is now set to {0}.", sc.Status.ToString());

            return 0;
        }
        catch
        {
            Console.WriteLine("No service by that name currently installed");
            return -1;
        }
    }
}