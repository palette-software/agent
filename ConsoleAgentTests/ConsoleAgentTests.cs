using System;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;

namespace ConsoleAgentTests
{
    [TestClass]
    public class ConsoleAgentTests
    {
        [TestMethod]
        public void TestManageMonitoredProcesses()
        {
            var _req = new HttpRequest();
            string jsonString = "{\"cpu-monitored-processes\": [ \"facebook\", \"whatsapp\", \"calendar\", \"sam\", \"max\" ] }";
            _req.JSON = fastJSON.JSON.Instance.Parse(jsonString) as Dictionary<string, object>;

            ProcessMonitoring pm = new ProcessMonitoring();
            pm.ManageCpuMonitors(_req);
            Assert.IsTrue(pm.cpuMonitoredProcesses.Count == 5);
            CollectionAssert.Contains(pm.cpuMonitoredProcesses.Keys, "facebook");
            CollectionAssert.Contains(pm.cpuMonitoredProcesses.Keys, "whatsapp");
            CollectionAssert.Contains(pm.cpuMonitoredProcesses.Keys, "calendar");
            CollectionAssert.Contains(pm.cpuMonitoredProcesses.Keys, "sam");
            CollectionAssert.Contains(pm.cpuMonitoredProcesses.Keys, "max");

            jsonString = "{\"cpu-monitored-processes\": [ \"facebook\", \"calendar\" ] }";
            _req.JSON = fastJSON.JSON.Instance.Parse(jsonString) as Dictionary<string, object>;
            pm.ManageCpuMonitors(_req);
            Assert.IsTrue(pm.cpuMonitoredProcesses.Count == 2);
            CollectionAssert.Contains(pm.cpuMonitoredProcesses.Keys, "facebook");
            CollectionAssert.Contains(pm.cpuMonitoredProcesses.Keys, "calendar");

            jsonString = "{\"cpu-monitored-processes\": [ \"dott\", \"calendar\", \"raptor\", \"prince-of-persia\" ] }";
            _req.JSON = fastJSON.JSON.Instance.Parse(jsonString) as Dictionary<string, object>;
            pm.ManageCpuMonitors(_req);
            Assert.IsTrue(pm.cpuMonitoredProcesses.Count == 4);
            CollectionAssert.Contains(pm.cpuMonitoredProcesses.Keys, "dott");
            CollectionAssert.Contains(pm.cpuMonitoredProcesses.Keys, "calendar");
            CollectionAssert.Contains(pm.cpuMonitoredProcesses.Keys, "raptor");
            CollectionAssert.Contains(pm.cpuMonitoredProcesses.Keys, "prince-of-persia");
        }
    }
}
