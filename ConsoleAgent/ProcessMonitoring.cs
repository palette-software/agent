using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

class ProcessMonitoring
{
    private readonly object lockPerfCounters = new object();
    private Dictionary<string, List<PerformanceCounter>> cpuMonitoredProcesses = new Dictionary<string, List<PerformanceCounter>>();
    private Dictionary<string, List<PerformanceCounter>> memoryMonitoredProcesses = new Dictionary<string, List<PerformanceCounter>>();
    internal static readonly string CPU_MONITORED_PROCESSES_KEY = "cpu-monitored-processes";
    internal static readonly string MEMORY_MONITORED_PROCESSES_KEY = "memory-monitored-processes";
    internal static readonly string CPU_COUNTER_NAME = "% Processor Time";
    internal static readonly string MEMORY_COUNTER_NAME = "Working Set - private";

    //This has to be put in each class for logging purposes
    private static readonly log4net.ILog logger = log4net.LogManager.GetLogger
    (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


    public void FillInMonitoredValues(ref List<object> list)
    {
        Monitor.Enter(lockPerfCounters);
        foreach (var sameNamedProcesses in cpuMonitoredProcesses.Values.Union(memoryMonitoredProcesses.Values))
        {
            if (sameNamedProcesses.Count == 0)
            {
                continue;
            }

            // Aggregate performance counter values for processes with the same name
            float counterSum = 0;
            foreach (PerformanceCounter counter in sameNamedProcesses)
            {
                try
                {
                    counterSum += counter.NextValue();
                }
                catch (Exception e)
                {
                    logger.WarnFormat("Failed to query value for performance counter: '{0}'! Exception: {1}", counter.InstanceName, e);
                }
            }

            PerformanceCounter firstCounter = sameNamedProcesses.ElementAt(0);
            if (firstCounter.CounterName.Equals(CPU_COUNTER_NAME))
            {
                // The "% Processor Time" performance counter for a given process returns a number based on
                // one logical CPU core, so in order to get the % for the total CPU we need to divide it
                // by the amount of logical CPU cores.
                counterSum /= Environment.ProcessorCount;
            }

            var data = PaletteHandler.MakeCounterResponse(firstCounter.CategoryName, firstCounter.CounterName,
                                                          firstCounter.InstanceName, counterSum);
            list.Add(data);
        }
        Monitor.Exit(lockPerfCounters);
    }

    public void ManageCpuMonitors(HttpRequest req)
    {
        ManageMonitoredProcesses(req, CPU_MONITORED_PROCESSES_KEY, CPU_COUNTER_NAME, ref cpuMonitoredProcesses);
    }

    public void ManageMemoryMonitors(HttpRequest req)
    {
        ManageMonitoredProcesses(req, MEMORY_MONITORED_PROCESSES_KEY, MEMORY_COUNTER_NAME, ref memoryMonitoredProcesses);
    }

    /// <summary>
    /// Manage the list of monitored processes based on the JSON body of the request,
    /// if it contains any instructions. If there is no instruction on that, the
    /// list of the monitored processes remains untouched.
    /// </summary>
    /// <param name="req"></param>
    public void ManageMonitoredProcesses(HttpRequest req, string key, string counterName, ref Dictionary<string, List<PerformanceCounter>> monitoredProcesses)
    {
        if (req == null)
        {
            logger.Warn("Request is NULL while attempting to manage monitored processes!");
            return;
        }

        var jsonBody = req.JSON;

        if (jsonBody == null)
        {
            // No process is mentioned for being monitored, do nothing.
            return;
        }

        if (!jsonBody.ContainsKey(CPU_MONITORED_PROCESSES_KEY))
        {
            // There is no instruction on monitored processes in the request's body
            return;
        }

        Monitor.Enter(lockPerfCounters);
        try
        {
            // Turn list of objects into list of strings
            List<object> parsedList = (List<object>)jsonBody[CPU_MONITORED_PROCESSES_KEY];
            List<string> newProcessList = new List<string>();
            foreach (var process in parsedList)
            {
                newProcessList.Add(process.ToString());
            }

            // Remove processes that are no longer being monitored
            var countersToRemove = monitoredProcesses.Keys.ToList().Where(x => !newProcessList.Contains(x));
            foreach (var processName in countersToRemove)
            {
                monitoredProcesses.Remove(processName);
            }

            // Make sure we monitor all the processes that have the same name as the ones, we are already monitoring
            foreach (var processMonitor in monitoredProcesses)
            {
                var sameNamedProcessCount = Process.GetProcessesByName(processMonitor.Key).Length;
                var processListLength = processMonitor.Value.Count;

                if (processListLength > sameNamedProcessCount)
                {
                    // Now there are less processes with same names than there were before.
                    processMonitor.Value.RemoveRange(sameNamedProcessCount, processListLength - sameNamedProcessCount);
                    continue;
                }

                // Getting here means that now there are less processes with same names than there were before.
                for (int i = processListLength; i < sameNamedProcessCount; i++)
                {
                    var processName = MakeCounterName(processMonitor.Key, i);
                    var counter = CreateProcessCounter(processName, counterName);
                    if (counter != null)
                    {
                        processMonitor.Value.Add(counter);
                    }
                }
            }

            // Add new monitored processes
            foreach (var process in newProcessList)
            {
                if (monitoredProcesses.ContainsKey(process))
                {
                    // This process is already being monitored.
                    continue;
                }

                var counterList = new List<PerformanceCounter>();
                for (int i = 0; i < Process.GetProcessesByName(process).Length; i++)
                {
                    var processName = MakeCounterName(process, i);
                    var counter = CreateProcessCounter(processName, counterName);
                    if (counter != null)
                    {
                        counterList.Add(counter);
                    }
                }
                // Insert the counter list even if it is empty now, becasue in later cycles it might
                // be expanded with processes starting later with this specific name.
                monitoredProcesses.Add(process, counterList);
            }
        }
        catch (Exception e)
        {
            logger.ErrorFormat("Error during managing monitored processes! Exception: {0}", e);
        }
        Monitor.Exit(lockPerfCounters);
    }

    private static string MakeCounterName(string process, int counter)
    {
        var processName = process;
        if (counter > 0)
        {
            // Create names like process#1, process#2, process#3...
            processName += "#" + counter;
        }

        return processName;
    }

    private static PerformanceCounter CreateProcessCounter(string processName, string counterName)
    {
        PerformanceCounter counter = null;
        try
        {
            counter = new PerformanceCounter("Process", counterName, processName);

            // Make sure that the new performance counter has a real value initially
            counter.NextValue();
        }
        catch (Exception e)
        {
            logger.WarnFormat("Failed to create '{0}' performance counter for process: '{1}'! Exception: {2}",
                counterName, processName, e);
            return null;

        }

        return counter;
    }
}
