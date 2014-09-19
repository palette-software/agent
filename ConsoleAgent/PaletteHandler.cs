using System;
using System.IO;
using System.Web;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using fastJSON;
using System.Text.RegularExpressions;
using log4net;
using log4net.Config;
using System.Data;
using System.Data.SqlClient;
using System.Data.Odbc;
using System.Security.Cryptography;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;

/// <summary>
/// Handles HTTP requests that come into agent.  Inherits from HttpHandler 
/// </summary>
class PaletteHandler : HttpHandler
{
    private Agent agent;

    //This has to be put in each class for logging purposes
    private static readonly log4net.ILog logger = log4net.LogManager.GetLogger
    (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="agent">Agent instance</param>
    public PaletteHandler(Agent agent)
    {
        // agent autorization parameters come from the agent instance.
        this.agent = agent;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public string GetRequiredJSONString(HttpRequest req, string name)
    {
        if (req.JSON == null) {
            throw new HttpBadRequest("Request must contain valid JSON.");
        }
        if (!req.JSON.ContainsKey(name)) {
            throw new HttpBadRequest("Missing JSON parameter '" + name + "'.");
        }
        try {
            return (string)req.JSON[name];
        } catch (Exception e) {
            throw new HttpBadRequest(e.ToString());
        }
    }

    /// <summary>
    /// Sorts requests based on URI
    /// </summary>
    /// <param name="req">HttpRequest</param>
    /// <returns>HttpResponse</returns>
    override public HttpResponse Handle(HttpRequest req)
    {
        switch (req.URI)
        {
            case "/ad":
                return HandleActiveDirectory(req);
            case "/archive":
                return HandleArchive(req);
            case "/auth":
                return HandleAuth(req);
            case "/cli":
                return HandleCmd(req);
            case "/file":
                return HandleFile(req);
            case "/hup":
                return HandleHUP(req);
            case "/maint":
                return HandleMaint(req);
            case "/ping":
                return HandlePing(req);
            case "/firewall":
                return HandleFirewall(req);
            case "/sql":
                return HandleSQL(req);
            default:
                throw new HttpNotFound();
        }
    }

    /// <summary>
    /// Handles a request to /ping
    /// returns no data, only a 200 OK
    /// </summary>
    /// <param name="req">Http request</param>
    /// <returns>HttpResponse</returns>
    private HttpResponse HandlePing(HttpRequest req)
    {
        HttpResponse res = req.Response;
        return res;
    }

    /// <summary>
    /// Handles a request to /auth
    /// </summary>
    /// <param name="req">Http request</param>
    /// <returns>HttpResponse</returns>
    private HttpResponse HandleAuth(HttpRequest req)
    {
        HttpResponse res = req.Response;
        req.ContentType = "application/json";

        UInt64 installedMemory = 0;
        MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();

        if (GlobalMemoryStatusEx(memStatus))
        {
            installedMemory = memStatus.ullTotalPhys;
        }

        Dictionary<string, object> data = new Dictionary<string, object>();
        data["license-key"] = agent.licenseKey;
        data["version"] = Agent.VERSION;
        data["os-version"] = System.Environment.OSVersion.ToString();
        data["os-bitness"] = Program.Is64BitOperatingSystem() ? 64 : 32;
        data["processor-type"] = System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
        data["processor-count"] = System.Environment.ProcessorCount.ToString();
        data["installed-memory"] = installedMemory;
        data["hostname"] = agent.hostname;
        data["fqdn"] = agent.GetFQDN();
        data["ip-address"] = agent.ipaddr;
        data["listen-port"] = agent.archivePort;
        data["uuid"] = agent.uuid;
        data["install-dir"] = agent.installDir;
        data["data-dir"] = agent.programDataDir;

        logger.Info("/auth");
        logger.Debug(data.ToString());

        res.Write(fastJSON.JSON.Instance.ToJSON(data));
        return res;
    }

    /// <summary>
    /// Handles a request to /cmd
    /// </summary>
    /// <param name="req">Http request</param>
    /// <returns>HttpResponse</returns>
    private HttpResponse HandleCmd(HttpRequest req)
    {
        HttpResponse res = req.Response;
        res.ContentType = "application/json";

        Dictionary<string, object> outputBody = null;

        UInt64 xid;
        if (req.Method == "POST")
        {
            xid = GetXidfromJSON(req);

            string action = GetAction(req);
            if (action == "start")
            {
                String cmd = GetCmd(req);
                logger.Info(String.Format("CMD[{0}]: {1}", xid, cmd));

                Dictionary<string, string> env = GetEnv(req);
                bool immediate = GetImmediate(req);

                agent.processManager.Start(xid, cmd, env, immediate);
                outputBody = agent.processManager.GetInfo(xid);
                if (immediate)
                {
                    /* Cleanup immediate commands to avoid an additional call from the controller. */
                    try
                    {
                        agent.processManager.Cleanup(xid);
                    }
                    catch (IOException e)
                    {
                        logger.Error(e.ToString());
                        outputBody.Add("error", e.ToString());
                    }
                }
            }
            else if (action == "cleanup")
            {
                outputBody = new Dictionary<string, object>();
                try
                {
                    agent.processManager.Cleanup(xid);
                }
                catch (Exception e)
                {
                    logger.Error(e.ToString());
                    outputBody.Add("error", e.ToString());
                }
                outputBody.Add("xid", xid);
            }
            else
            {
                throw new HttpBadRequest("Invalid action value in JSON POST");
            }
        }
        else if (req.Method == "GET")
        {
            xid = GetXidfromQueryString(req);
            outputBody = agent.processManager.GetInfo(xid);
        }
        else
        {
            throw new HttpMethodNotAllowed();
        }
        string json = fastJSON.JSON.Instance.ToJSON(outputBody);
        if (outputBody.ContainsKey("run-status") && ((string)(outputBody["run-status"]) == "finished"))
        {
            logger.Info("JSON: " + json);
        }
        else
        {
            logger.Debug("JSON: " + json);
        }
        res.Write(json);
        return res;
    }

    /// <summary>
    /// Handles a request to /maint
    /// </summary>
    /// <param name="req">Http request</param>
    /// <returns>HttpResponse</returns>
    private HttpResponse HandleMaint(HttpRequest req)
    {
        HttpResponse res = req.Response;
        res.ContentType = "application/json";

        ServerControlInfo info;
        try
        {
            info = ServerControlInfo.parse(req);
        }
        catch (ArgumentException e)
        {
            throw new HttpBadRequest(e.Message);
        }

        if (info.port != -1)
        {
            agent.maintPort = info.port;
        }

        switch (info.action)
        {
            case "start":
                agent.startMaintServer();
                break;
            case "stop":
                agent.stopMaintServer();
                break;
        }

        res.Write("{\"status\": \"ok\", \"port\": " + Convert.ToString(agent.maintPort) + "}");
        return res;
    }

    /// <summary>
    /// Gets the user's status in Active Directory
    /// </summary>
    /// <param name="userName">user name in domain</param>
    /// <param name="password">password in domain</param>
    /// <returns>True or False (exception if no domain)</returns>
    static bool ActiveDirectoryValidate(string domain, string userName, string password)
    {
        // create a "principal context" - e.g. your domain (could be machine, too)
        using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, domain))
        {
            // validate the credentials                    
            return pc.ValidateCredentials(userName, password);
        }
        // Current security context is not associated with an Active Directory domain or forest
        // throws System.DirectoryServices.ActiveDirectory.ActiveDirectoryOperationException
        // return false;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    private HttpResponse HandleActiveDirectory(HttpRequest req)
    {
        HttpResponse res = req.Response;
        res.ContentType = "application/json";

        string domain = GetRequiredJSONString(req, "domain");
        string userName = GetRequiredJSONString(req, "username");
        string password = GetRequiredJSONString(req, "password");

        logger.Info("activedirectory : " + userName);

        Dictionary<string, object> d = new Dictionary<string, object>();

        bool status;
        try
        {
            status = ActiveDirectoryValidate(domain, userName, password);
            if (status)
            {
                d["status"] = "OK";
            } else {
                d["status"] = "FAILED";
                d["error"] = "Login failed; Invalid userID or password.";
            }
        }
        catch (System.DirectoryServices.ActiveDirectory.ActiveDirectoryOperationException e)
        {
            d["status"] = "FAILED";
            //d["error"] = "Current security context is not associated with an Active Directory domain";
            d["error"] = e.ToString();
        }

        string json = fastJSON.JSON.Instance.ToJSON(d);
        logger.Debug("JSON: " + json);
        res.Write(json);
        return res;
    }

    /// <summary>
    /// Handles a request to /archive
    /// </summary>
    /// <param name="req">Http request</param>
    /// <returns>HttpResponse</returns>
    private HttpResponse HandleArchive(HttpRequest req)
    {
        HttpResponse res = req.Response;
        res.ContentType = "application/json";

        ServerControlInfo info;
        try
        {
            info = ServerControlInfo.parse(req);
        }
        catch (ArgumentException e)
        {
            throw new HttpBadRequest(e.Message);
        }

        if (info.port != -1)
        {
            agent.archivePort = info.port;
        }
    
        switch (info.action)
        {
            case "start":
                agent.startArchiveServer();
                break;
            case "stop":
                agent.stopArchiveServer();
                break;
        }
        
        res.Write("{\"status\": \"ok\", \"port\": " + Convert.ToString(agent.archivePort) + "}");
        return res;
    }

    /// <summary>
    /// Handles a request to /firewall
    /// </summary>
    /// <param name="req">Http request</param>
    /// <returns>HttpResponse</returns>
    private HttpResponse HandleFirewall(HttpRequest req)
    {
        HttpResponse res = req.Response;
        res.ContentType = "application/json";

        if (req.Method == "POST")  //Enable ports
        {
            if (!req.JSON.ContainsKey("ports"))
            {
                throw new HttpBadRequest("Missing 'port' attribute in JSON body.");
            }

            List<int> portsToEnable = new List<int>();
            List<int> portsToDisable = new List<int>();

            // FIXME: try block and check for valid actions.
            List<Object> portList = (List<Object>)req.JSON["ports"];
            string msg = "Firewall:";
            foreach (Object obj in portList)
            {
                Dictionary<string, object> d = (Dictionary<string, object>)obj;
                int port = Convert.ToUInt16(d["num"]); // FIXME //
                switch (d["action"].ToString().ToLower())
                {
                    case "enable":
                        portsToEnable.Add(port);
                        msg += " enable:" + Convert.ToString(port);
                        break;
                    case "disable":
                        portsToDisable.Add(port);
                        msg += " disable:" + Convert.ToString(port);
                        break;
                }
            }

            FirewallUtil fUtil = new FirewallUtil();
            fUtil.OpenFirewall(portsToEnable);
            fUtil.CloseFirewall(portsToDisable);

            if (portsToEnable.Count + portsToDisable.Count == 0)
            {
                throw new HttpBadRequest("port list was empty.");
            }
            logger.Info(msg);
        }
        else if (req.Method == "GET")
        {
            FirewallUtil fUtil = new FirewallUtil();
            List<int> openPorts = fUtil.CheckPorts();

            string json = GetPortInformationInJSON(openPorts);
            logger.Info("JSON: " + json);
            res.Write(json);
            return res;
        }

        res.Write("{\"status\": \"ok\"}");
        return res;
    }

    private HttpResponse HandleFileGET(HttpRequest req, string path)
    {
        HttpResponse res = req.Response;

        if (!File.Exists(path))
        {
            throw new HttpGone();
        }

        // FIXME: buffered copy
        res.Write(File.ReadAllBytes(path));
        return res;
    }

    private string ComputeSHA256(byte [] data)
    {
        byte[] hash = SHA256.Create().ComputeHash(data);
        
        string hashString = string.Empty;
        foreach (byte x in hash)
        {
            hashString += String.Format("{0:x2}", x);
        }
        return hashString;
    }

    private Dictionary<string, object> HandleSHA256(HttpRequest req)
    {
        string path = GetRequiredJSONString(req, "path");

        Dictionary<string, object> d = new Dictionary<string, object>();
        if (!File.Exists(path))
        {
            d["status"] = "FAILED";
            d["error"] = "File does not exist: " + path;
            return d;
        }
        byte[] data = File.ReadAllBytes(path);
        string hash = ComputeSHA256(data);

        d["status"] = "OK";
        d["hash"] = hash;
        return d;
    }

    private Dictionary<string, object> HandleMOVE(HttpRequest req)
    {
        string src = GetRequiredJSONString(req, "source");
        string dst = GetRequiredJSONString(req, "destination");

        Dictionary<string, object> d = new Dictionary<string, object>();

        try {
            File.Move(src, dst);
        } catch (Exception e){
            d["status"] = "FAILED";
            d["error"] = e.ToString();
            return d;
        }
        d["status"] = "OK";
        return d;
    }

    private Dictionary<string, object> HandleLISTDIR(HttpRequest req)
    {
        string path = GetRequiredJSONString(req, "path");

        Dictionary<string, object> d = new Dictionary<string, object>();
        if (!Directory.Exists(path))
        {
            d["status"] = "FAILED";
            d["error"] = "Not a valid directory: '" + path + "'";
            return d;
        }

        DirectoryInfo di = new DirectoryInfo(path);
        
        FileInfo[] fis = di.GetFiles();
        string[] files = new string[fis.Length];
        for (int i = 0; i < fis.Length; i++)
        {
            files[i] = fis[i].Name;
        }

        DirectoryInfo[] dis = di.GetDirectories();
        string[] dirs = new string[dis.Length];
        for (int i = 0; i < dis.Length; i++)
        {
            dirs[i] = dis[i].Name;
        }
        d["status"] = "OK";
        d["files"] = files;
        d["directories"] = dirs;
        return d;
    }

    private Dictionary<string, object> HandleFILESIZE(HttpRequest req)
    {
        string path = GetRequiredJSONString(req, "path");

        Dictionary<string, object> d = new Dictionary<string, object>();
        if (!File.Exists(path))
        {
            d["status"] = "FAILED";
            d["error"] = "Invalid path: '" + path + "'";
            return d;
        }

        FileInfo fi = new FileInfo(path);
        d["status"] = "OK";
        d["size"] = fi.Length;
        return d;
    }

    private Dictionary<string, object> HandleMKDIRS(HttpRequest req)
    {
        string path = GetRequiredJSONString(req, "path");

        Dictionary<string, object> d = new Dictionary<string, object>();
        Directory.CreateDirectory(path);
        d["status"] = "OK";
        return d;
    }

    private HttpResponse HandleFilePOST(HttpRequest req)
    {
        Dictionary<string, object> outputBody = null;
        HttpResponse res = req.Response;

        string action = GetRequiredJSONString(req, "action").ToUpper();
        logger.Info(req.Method + " /file : " + action);

        switch (action)
        {
            case "SHA256":
                outputBody = HandleSHA256(req);
                break;
            case "MOVE":
                outputBody = HandleMOVE(req);
                break;
            case "LISTDIR":
                outputBody = HandleLISTDIR(req);
                break;
            case "FILESIZE":
                outputBody = HandleFILESIZE(req);
                break;
            case "MKDIRS":
                outputBody = HandleMKDIRS(req);
                break;
            default:
                throw new HttpBadRequest("Invalid action : " + action);
        }

        string json = fastJSON.JSON.Instance.ToJSON(outputBody);
        logger.Debug("JSON: " + json);
        res.Write(json);
        return res;
    }

    private HttpResponse HandleFilePUT(HttpRequest req, string path)
    {
        HttpResponse res = req.Response;
        // FIXME: buffered copy
        File.WriteAllBytes(path, req.data);
        return res;
    }

    private HttpResponse HandleFileDELETE(HttpRequest req, string path)
    {
        
        HttpResponse res = req.Response;

        Dictionary<string, object> outputBody = new Dictionary<string, object>();
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        } catch (Exception e)
        {
            logger.Error(e.ToString());
            outputBody.Add("error", e.ToString());
        }

        string json = fastJSON.JSON.Instance.ToJSON(outputBody);
        res.Write(json);
        return res;
    }

    private HttpResponse HandleFile(HttpRequest req)
    {
        /* POST is unique in that it doesn't get 'path' from the query string. */
        if (req.Method == "POST")
        {
            return HandleFilePOST(req);
        }

        string path = req.QUERY["path"];
        if (path == null)
        {
            throw new HttpBadRequest("The 'path' must be specified in the query string.");
        }

        logger.Info(req.Method + " /file : " + path);

        switch (req.Method)
        {
            case "GET":
                return HandleFileGET(req, path);
            case "PUT":
                return HandleFilePUT(req, path);
            case "DELETE":
                return HandleFileDELETE(req, path);
            default:
                throw new HttpMethodNotAllowed();
        }
    }

    private HttpResponse HandleHUP(HttpRequest req)
    {
        if (req.Method != "POST")
        {
            throw new HttpMethodNotAllowed();
        }

        HttpResponse res = req.Response;
        res.needRestart = true;
        return req.Response;
    }

    /// <summary>
    /// Handles SQL Requests of type GET or POST.  For POST, must contain a JSON object with keys 'connection' and 'select-statement'.
    ///  Sample working connection string: "DRIVER={PostgreSQL ANSI(x64)}; Server=127.0.0.1; Port=8060; Database=workgroup; Uid=tblwgadmin; Pwd=;"
    ///  See www.ConnectionStrings.com for ODBC examples
    /// </summary>
    /// <param name="req">The HTTP Request</param>
    /// <returns>For GET, returns possible data sources and sample connection string.  For POST, returns a DataTable serialized in JSON</returns>
    private HttpResponse HandleSQL(HttpRequest req)
    {
        //SAMPLE JSON: {'dbtype': 'postgres', 'server': '127.0.0.1', 'port': '8060', 'database': 'workgroup', 'uid': 'tblwgadmin', 'pwd': '', 'statement': 'SELECT * FROM table'}
        // use only connection string and statement

        HttpResponse res = req.Response;
        string json = "";
        string connectionString = "";
        string selectStatement = "";

        if (req.Method == "POST")  //Return a Data table in JSON format, leave possibility of "PUT" to update or insert data
        {
            if (req.JSON == null)
            {
                throw new HttpBadRequest(@"SQL POST Request must contain JSON. \n");
            }

            if (req.JSON.ContainsKey("connection") && req.JSON.ContainsKey("select-statement"))
            {
                connectionString = req.JSON["connection"].ToString();
                selectStatement = req.JSON["select-statement"].ToString();
            }
            else
            {
                logger.Debug("Incoming JSON: " + req.JSON);
                throw new HttpBadRequest(@"SQL Request must contain a JSON object with keys 'connectString' and 'queryString'.");
            }

            try
            {
                DataTable dt = GetDataTableFromPostgreSQLDB(connectionString, selectStatement);
                json = fastJSON.JSON.Instance.ToJSON(dt);
            }
            catch (OdbcException ex)
            {
                string errMsg = ex.ToString();
                logger.Error(errMsg);
                Dictionary<string, object> d = new Dictionary<string, object>();
                d["error"] = errMsg;
                json = fastJSON.JSON.Instance.ToJSON(d);
            }
        }
        else if (req.Method == "GET")  //return data sources
        {
            string dataSources = @"ODBC Data Drivers = {PostgreSQL ANSI(x64)}, {PostgreSQL UNICODE(x64)}\n" +
                "Sample Connection String: 'DRIVER={PostgreSQL ANSI(x64)}; Server=127.0.0.1; Port=8060; Database=workgroup; Uid=tblwgadmin; Pwd=;' \n";

            res.Write(File.ReadAllText(dataSources));
        }
        else
        {
            throw new HttpMethodNotAllowed();
        }

        logger.Info("JSON: " + json);

        res.ContentType = "application/json";
        res.Write(json);

        return res;
    }

    /// <summary>
    /// Pulls xid (agent process id) from JSON dictionary
    /// </summary>
    /// <param name="req">HttpRequest</param>
    /// <returns>xid</returns>
    private UInt64 GetXidfromJSON(HttpRequest req)
    {
        UInt64 xid;

        if (req.JSON == null)
        {
            throw new HttpBadRequest("Request must be JSON.\n");
        }

        if (!req.JSON.ContainsKey("xid"))
        {
            throw new HttpBadRequest("Missing 'xid' in JSON");
        }

        try
        {
            xid = Convert.ToUInt64(req.JSON["xid"]);
        } catch {
            throw new HttpBadRequest("Invalid 'xid': "+req.JSON["xid"]);
        }

        return xid;
    }

    /// <summary>
    /// Gets list of ports and actions to perform from JSON dictionary
    /// </summary>
    /// <param name="req">HttpRequest</param>
    /// <returns>xid</returns>
    private Dictionary<int, string> GetPortDictionaryfromJSON(HttpRequest req)
    {
        if (req.JSON == null) throw new ArgumentNullException();

        
        //{
        //    object objList = fastJSON.JSON.Instance.ToObject<Dictionary<string, Dictionary<int, string>>>(req.JSON);

        //    Dictionary<string, Dictionary<int, string>> portList = (Dictionary<string, Dictionary<int, string>>)objList;

        //    if (portList.ContainsKey("ports")) return portList["ports"];
        //}
        return null;
    }

    /// <summary>
    /// Returns the port information in JSON, i.e. {"ports": [num:8889, "state": "open"]}
    /// </summary>
    /// <returns>JSON formatted string</returns>
    private string GetPortInformationInJSON(List<int> openPorts)
    {
        Dictionary<string, Object> data = new Dictionary<string, Object>();
        List<Object> list = new List<Object>();

        foreach (int portNum in openPorts)
        {
            Dictionary<string, object> portInfo = new Dictionary<string, object>();
            portInfo.Add("num", portNum);
            portInfo.Add("state", "open");
            list.Add(portInfo);
        }
        data.Add("ports", list);

        string json = fastJSON.JSON.Instance.ToJSON(data);

        return json;
    }
    /// Pulls xid (agent process id) from query string
    /// </summary>
    /// <param name="req">HttpRequest</param>
    /// <returns>xid</returns>
    private UInt64 GetXidfromQueryString(HttpRequest req)
    {
        try
        {
            return Convert.ToUInt64(req.QUERY["xid"]);
        }
        catch
        {
            throw new HttpBadRequest("Invalid 'xid' in QUERY_STRING\n");
        }
    }

    /// <summary>
    /// Pulls action from JSON dictionary
    /// </summary>
    /// <param name="req">HttpRequest</param>
    /// <returns>action</returns>
    private string GetAction(HttpRequest req)
    {
        if (req.JSON.ContainsKey("action"))
        {
            string action = req.JSON["action"].ToString();
            return action;
        }
        else
        {
            throw new HttpBadRequest("missing parameter : 'action'");
        }
    }

    /// <summary>
    /// Pulls CLI Command from JSON dictionary
    /// </summary>
    /// <param name="req">HttpRequest</param>
    /// <returns>Command</returns>
    private string GetCmd(HttpRequest req)
    {
        if (req.JSON != null)
        {
            string cmd = req.JSON["cli"].ToString();
            return cmd;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Return the process environment as a Dictionary - if specified.
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    private Dictionary<string, string> GetEnv(HttpRequest req)
    {
        if ((req.JSON == null) || (!req.JSON.ContainsKey("env")))
        {
            return null;
        }

        Dictionary<string, string> result = new Dictionary<string, string>();

        var obj = req.JSON["env"];
        try
        {
            Dictionary<string, object> data = (Dictionary<string, object>)obj;
            foreach (KeyValuePair<string, object> entry in data)
            {
                result.Add(entry.Key, entry.Value.ToString());
            }
        }
        catch
        {
            throw new HttpBadRequest("Invalid 'env' specified : " + obj.ToString());
        }

        return result;
    }

    private bool GetImmediate(HttpRequest req)
    {
        if ((req.JSON == null) || (!req.JSON.ContainsKey("immediate")))
        {
            return false;
        }

        try {
            return (bool)(req.JSON["immediate"]);
        } catch {}
        return false;
    }

    /// <summary>
    /// Returns a DataTable for a given query and connection string (ODBC)
    /// </summary>
    /// <param name="pgConnect">Connection String</param>
    /// <param name="query">Query</param>
    /// <returns>A DataTable</returns>
    private static DataTable GetDataTableFromPostgreSQLDB(string connectionString, string query)
    {
        // Attempt to open a connection
        OdbcConnection con = new OdbcConnection(connectionString);
        DataTable dt = new DataTable();

        try
        {
            con.Open();
            OdbcCommand cmd = new OdbcCommand(query, con);

            OdbcDataAdapter sqlDa = new OdbcDataAdapter(cmd);

            sqlDa.Fill(dt);
        }
        finally
        {
            con.Close();
        }

        return dt;
    }

    /// <summary>
    /// Helper class for /maint and /archive URIs.
    /// </summary>
    private class ServerControlInfo
    {
        public string action = null;
        public int port = -1;
        public bool https = false;

        public static ServerControlInfo parse(HttpRequest req)
        {
            if (req.Method != "POST")
            {
                throw new HttpMethodNotAllowed();
            }

            ServerControlInfo info = new ServerControlInfo();
            foreach (string key in req.JSON.Keys)
            {
                object val = req.JSON[key];
                switch (key.ToLower())
                {
                    case "action":
                        info.action = val.ToString().ToLower();
                        if (info.action != "start" && info.action != "stop")
                        {
                            throw new ArgumentException("invalid action : " + info.action);
                        }
                        break;
                    case "port":
                        info.port = Convert.ToInt32(val);
                        break;
                    case "https":
                        info.https = Convert.ToBoolean(val);
                        break;
                    default:
                        throw new ArgumentException("invalid parameter : " + key);
                }
            }
            return info;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
        public MEMORYSTATUSEX()
        {
            this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);
}