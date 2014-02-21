using System;
using System.Web;
using System.Collections.Generic;
using fastJSON;
using System.Text.RegularExpressions;

/// <summary>
/// Handles HTTP requests that come into agent.  Inherits from HttpHandler 
/// </summary>
class PaletteHandler : HttpHandler
{
    private Agent agent;

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
        data["listen-port"] = agent.archiveListenPort;
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
                Console.WriteLine(cmd);
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
        Console.WriteLine(json);
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

        // FIXME: verify the request.
        string action = GetAction(req);
        if (action == "start")
        {
            agent.startMaintServer();
            Console.WriteLine("Maintenance webserver started.");
        }
        else if (action == "stop")
        {
            agent.stopMaintServer();
            Console.WriteLine("Maintenance webserver stopped.");
        }
        else
        {
            throw new HttpBadRequest("invalid action");
        }

        res.Write("{\"status\": \"ok\"}");
        return res;
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
            case "/auth":
                return HandleAuth(req);
            case "/cli":
                return HandleCmd(req);
            case "/maint":
                return HandleMaint(req);
            default:
                throw new HttpNotFound();
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
            return "unknown";
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
}
