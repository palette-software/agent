using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

class ProcessMonitoring
{
    private readonly object lockPerfCounters = new object();
    private List<PerformanceCounter> processCounters = new List<PerformanceCounter>();
    internal static readonly string MONITORED_PROCESSES_KEY = "monitored-processes";

    //This has to be put in each class for logging purposes
    private static readonly log4net.ILog logger = log4net.LogManager.GetLogger
    (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public void FillInMonitoredValues(ref List<object> list)
    {
        Monitor.Enter(lockPerfCounters);

        foreach (PerformanceCounter counter in processCounters)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data["category-name"] = counter.CategoryName;
            data["counter-name"] = counter.CounterName;
            data["instance-name"] = counter.InstanceName;
            try
            {
                // The "% Processor Time" performance counter for a given process returns a number based on
                // one logical CPU core, so in order to get the % for the total CPU we need to divide it
                // by the amount of logical CPU cores.
                data["value"] = counter.NextValue() / Environment.ProcessorCount;
                list.Add(data);
            }
            catch (Exception e)
            {
                logger.WarnFormat("Failed to query value for performance counter: '{0}'! Exception: {1}", counter.InstanceName, e);
            }
        }
        Monitor.Exit(lockPerfCounters);
    }

    /// <summary>
    /// Manage the list of monitored processes based on the JSON body of the request,
    /// if it contains any instructions. If there is no instruction on that, the
    /// list of the monitored processes remains untouched.
    /// </summary>
    /// <param name="req"></param>
    public void ManageMonitoredProcesses(HttpRequest req)
    {
        if (req == null)
        {
            return;
        }

        var jsonBody = req.JSON;

        if (jsonBody == null)
        {
            // No process is mentioned for being monitored, do nothing.
            return;
        }

        if (!jsonBody.ContainsKey(MONITORED_PROCESSES_KEY))
        {
            // There is no instuction on monitored processes in the request's body
            return;
        }

        try
        {
            // Turn list of objects into list of strings
            List<object> parsedList = (List<object>)jsonBody[MONITORED_PROCESSES_KEY];
            List<string> processList = new List<string>();
            foreach (var process in parsedList)
            {
                processList.Add(process.ToString());
            }

            // Remove processes that are no longer being monitored
            processCounters.RemoveAll(x => x.CategoryName.Equals("Process") && !processList.Contains(x.InstanceName));

            // Add new monitored processes. First strip those that are already being monitored...
            processList.RemoveAll(x =>
                {
                    foreach (var counter in processCounters)
                    {
                        if (counter.CategoryName.Equals("Process") && counter.InstanceName.Equals(x))
                        {
                            return true;
                        }
                    }

                    return false;
                }
            );
            // ... then add the remaining ones as new counters.
            foreach (var process in processList)
            {
                try
                {
                    // TODO : add processes with same name too!
                    var counter = new PerformanceCounter("Process", "% Processor Time", process);
                    processCounters.Add(counter);

                    // Make sure that the new performance counter has a real value initially
                    counter.NextValue();
                }
                catch (Exception e)
                {
                    logger.WarnFormat("Failed to add processor performance counter for process: '{0}'! Exception: {1}",
                        process, e);
                }
            }
        }
        catch (Exception e)
        {
            logger.ErrorFormat("Error during managing monitored processes! Exception: {o}", e);
        }
    }
}
