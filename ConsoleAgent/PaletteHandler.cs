﻿using System;
using System.IO;
using System.Web;
using System.Collections.Generic;
using fastJSON;
using System.Text.RegularExpressions;
using log4net;
using log4net.Config;

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
    /// Sorts requests based on URI
    /// </summary>
    /// <param name="req">HttpRequest</param>
    /// <returns>HttpResponse</returns>
    override public HttpResponse Handle(HttpRequest req)
    {
        switch (req.URI)
        {
            case "/archive":
                return HandleArchive(req);
            case "/auth":
                return HandleAuth(req);
            case "/cli":
                return HandleCmd(req);
            case "/file":
                return HandleFile(req);
            case "/maint":
                return HandleMaint(req);
            case "/ping":
                return HandlePing(req);
            case "/firewall":
                return HandleFirewall(req);
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

        Dictionary<string, object> data = new Dictionary<string, object>();
        data["username"] = agent.username;
        data["password"] = agent.password;
        data["version"] = Agent.VERSION;
        data["hostname"] = agent.hostname;
        data["type"] = agent.type;
        data["ip-address"] = agent.ipaddr;
        data["listen-port"] = agent.archivePort;
        data["uuid"] = agent.uuid;
        data["install-dir"] = agent.installDir;
        data["displayname"] = agent.displayName;
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

        Dictionary<string, string> d = new Dictionary<string, string>();
        Dictionary<string, object> outputBody = null;

        int xid = -1;
        if (req.Method == "POST")
        {
            xid = GetXidfromJSON(req);

            string action = GetAction(req);
            if (action == "start")
            {
                string cmd = GetCmd(req);
                logger.Info("CMD: " + cmd);

                agent.processManager.Start(xid, cmd);
                outputBody = agent.processManager.GetInfo(xid);
            }
            else if (action == "cleanup")
            {
                agent.processManager.Cleanup(xid);
                outputBody = new Dictionary<string, object>();
                outputBody.Add("xid", xid);
            }
            else
            {
                throw new SystemException("Invalid action value in JSON post");
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
        logger.Info("JSON: " + json);
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
            throw new HttpNotFound();
        }

        res.Write(File.ReadAllText(path));
        return res;
    }

    private HttpResponse HandleFilePUT(HttpRequest req, string path)
    {
        HttpResponse res = req.Response;
        File.WriteAllText(path, req.data);
        return res;
    }

    private HttpResponse HandleFileDELETE(HttpRequest req, string path)
    {
        HttpResponse res = req.Response;
        File.Delete(path);
        return res;
    }

    private HttpResponse HandleFile(HttpRequest req)
    {
        string path = req.QUERY["path"];
        if (path == null)
        {
            throw new HttpBadRequest("The 'path' must be specified in the query string.");
        }

        logger.Debug(req.Method + " /file : " + path);

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

    /// <summary>
    /// Pulls xid (agent process id) from JSON dictionary
    /// </summary>
    /// <param name="req">HttpRequest</param>
    /// <returns>xid</returns>
    private int GetXidfromJSON(HttpRequest req)
    {
        if (req.JSON != null)
        {
            long xid = (long)req.JSON["xid"];
            return (int)xid;
        }
        else
        {
            int xid = -1;
            string[] tokens = req.Url.Split('?');
            if (tokens[1].Contains("xid"))
            {
                string[] subtokens = tokens[1].Split('=');
                if (subtokens.Length == 2)
                {                    
                    Int32.TryParse(subtokens[1], out xid);                    
                }
            }
            return xid;
        }
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
    private int GetXidfromQueryString(HttpRequest req)
    {
        // FIXME: test for XID existence.
        return Convert.ToInt32(req.QUERY["xid"]);
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
            throw new ArgumentException("missing parameter : \"action\"");
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
    /// Helper class for /maint and /archive URIs.
    /// </summary>
    private class ServerControlInfo
    {
        public string action = null;
        public int port = -1;

        public static ServerControlInfo parse(HttpRequest req)
        {
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
                    default:
                        throw new ArgumentException("invalid parameter : " + key);
                }
            }
            return info;
        }
    }
}