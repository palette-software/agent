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
            var _agent = Substitute.For<Agent>("fake_config.ini");

            //var _stream = Substitute.For<Stream>();
            //_stream.ReadByte().Returns(-1);
            //var _reader = Substitute.For<HttpStreamReader>(_stream);
            //var testString = "This is a beginning of a ....";
            //_reader.ReadLine().Returns(testString);
            //var _req = HttpRequest.Get(_reader);

            var _req = new HttpRequest();
            string jsonString = "{\"monitored-processes\": [ \"facebook\", \"whatsapp\", \"calendar\" ] }";
            _req.JSON = fastJSON.JSON.Instance.Parse(jsonString) as Dictionary<string, object>;
            //List<string> processList = new List<string>();
            //processList.Add("facebook");
            //processList.Add("whatsapp");
            //processList.Add("calendar");
            //_req.JSON = new Dictionary<string, object>();
            //_req.JSON.Add(PaletteHandler.MONITORED_PROCESSES_KEY, processList);

            ProcessMonitoring pm = new ProcessMonitoring();
            pm.ManageCpuMonitors(_req);
        }
    }
}
