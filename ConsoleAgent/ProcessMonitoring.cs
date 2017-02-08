using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

class ProcessMonitoring
{
    private readonly object lockPerfCounters = new object();
    private Dictionary<string, List<PerformanceCounter>> monitoredCounters = new Dictionary<string, List<PerformanceCounter>>();
    internal static readonly string MONITORED_PROCESSES_KEY = "monitored-processes";

    //This has to be put in each class for logging purposes
    private static readonly log4net.ILog logger = log4net.LogManager.GetLogger
    (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


    public void FillInMonitoredValues(ref List<object> list)
    {
        Monitor.Enter(lockPerfCounters);
        foreach (var sameNamedProcesses in monitoredCounters.Values)
        {
            if (sameNamedProcesses.Count == 0)
            {
                continue;
            }

            PerformanceCounter firstCounter = sameNamedProcesses.ElementAt(0);
            Dictionary<string, object> data = new Dictionary<string, object>();
            data["category-name"] = firstCounter.CategoryName;
            data["counter-name"] = firstCounter.CounterName;
            data["instance-name"] = firstCounter.InstanceName;

            // Aggregate the CPU consumption for processes with the same name
            float cpuCounterSum = 0;
            foreach (PerformanceCounter counter in sameNamedProcesses)
            {
                try
                {
                    // The "% Processor Time" performance counter for a given process returns a number based on
                    // one logical CPU core, so in order to get the % for the total CPU we need to divide it
                    // by the amount of logical CPU cores.
                    cpuCounterSum += counter.NextValue() / Environment.ProcessorCount;
                }
                catch (Exception e)
                {
                    logger.WarnFormat("Failed to query value for performance counter: '{0}'! Exception: {1}", counter.InstanceName, e);
                }
            }

            data["value"] = cpuCounterSum;
            list.Add(data);
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

        Monitor.Enter(lockPerfCounters);
        try
        {
            // Turn list of objects into list of strings
            List<object> parsedList = (List<object>)jsonBody[MONITORED_PROCESSES_KEY];
            List<string> newProcessList = new List<string>();
            foreach (var process in parsedList)
            {
                newProcessList.Add(process.ToString());
            }

            // Remove processes that are no longer being monitored
            var countersToRemove = monitoredCounters.Keys.Where(x => !newProcessList.Contains(x));
            foreach (var processName in countersToRemove)
            {
                if (!newProcessList.Contains(processName))
                {
                    monitoredCounters.Remove(processName);
                }
            }

            // Make sure we monitor all the processes that have the same name as the ones, we are already monitoring
            foreach (var processMonitor in monitoredCounters)
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
                    var counter = CreateProcessCpuCounter(processName);
                    if (counter != null)
                    {
                        processMonitor.Value.Add(counter);
                    }
                }
            }

            // Add new monitored processes. First strip those that are already being monitored...
            newProcessList.RemoveAll(x => monitoredCounters.ContainsKey(x));

            // ... then add the remaining ones as new counters.
            foreach (var process in newProcessList)
            {
                var counterList = new List<PerformanceCounter>();
                for (int i = 0; i < Process.GetProcessesByName(process).Length; i++)
                {
                    var processName = MakeCounterName(process, i);
                    var counter = CreateProcessCpuCounter(processName);
                    if (counter != null)
                    {
                        counterList.Add(counter);
                    }
                }
                // Insert the counter list even if it is empty now, becasue in later cycles it might
                // be expanded with processes starting later with this specific name.
                monitoredCounters.Add(process, counterList);
            }
        }
        catch (Exception e)
        {
            logger.ErrorFormat("Error during managing monitored processes! Exception: {o}", e);
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

    private static PerformanceCounter CreateProcessCpuCounter(string processName)
    {
        PerformanceCounter counter = null;
        try
        {
            counter = new PerformanceCounter("Process", "% Processor Time", processName);

            // Make sure that the new performance counter has a real value initially
            counter.NextValue();
        }
        catch (Exception e)
        {
            logger.WarnFormat("Failed to creamte CPU performance counter for process: '{0}'! Exception: {1}",
                processName, e);
            return null;

        }

        return counter;
    }
}
