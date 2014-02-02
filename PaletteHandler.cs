using System;
using System.Web;
using System.Collections.Generic;
using fastJSON;
using System.Text.RegularExpressions;

class PaletteHandler : HttpHandler
{
    protected Agent agent;
    protected string username = null;
    protected string type = null;
   
    protected string uuid = null;

    public PaletteHandler(Agent agent)
    {
        // agent autorization parameters come from the agent instance.
        this.agent = agent;
    }

    protected HttpResponse HandleAuth(HttpRequest req)
    {
        HttpResponse res = req.Response;
        req.ContentType = "application/json";

        Dictionary<string, object> data = new Dictionary<string, object>();
        data["username"] = agent.username;
        data["password"] = agent.password;
        data["version"] = Agent.VERSION;
        data["hostname"] = agent.host;
        data["type"] = agent.type;
        data["ip-address"] = agent.addr.ToString();
        data["listen-port"] = agent.port;
        data["uuid"] = uuid;

        res.Write(fastJSON.JSON.Instance.ToJSON(data));
        return res;
    }

    protected HttpResponse HandleCmd(HttpRequest req)
    {
        HttpResponse res = req.Response;
        res.ContentType = "application/json";

        Dictionary<string, string> d = new Dictionary<string, string>();
        Dictionary<string, object> outputBody = null;

        int xid = -1;
        if (req.Method == "POST")
        {
            xid = GetXidfromJSON(req);

            // FIXME: check for existence.
            string action = GetAction(req);
            if (action == "start")
            {
                string cmd = GetCmd(req);
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
                // FIXME: throw error.
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

    override public HttpResponse Handle(HttpRequest req)
    {
        switch (req.URI)
        {
            case "/auth":
                return HandleAuth(req);
            case "/cli":
                return HandleCmd(req);
            default:
                throw new HttpNotFound();
        }
    }

    protected int GetXidfromJSON(HttpRequest req)
    {
        // FIXME: test for XID existence.
        long xid = (long)req.JSON["xid"];
        return (int)xid;
    }

    protected int GetXidfromQueryString(HttpRequest req)
    {
        // FIXME: test for XID existence.
        return Convert.ToInt32(req.QUERY["xid"]);
    }

    protected string GetAction(HttpRequest req)
    {
        // FIXME: test for action existence.
        string action = req.JSON["action"].ToString();
        return action;
    }

    protected string GetCmd(HttpRequest req)
    {
        // FIXME: test for cli existence.
        string cmd = req.JSON["cli"].ToString();
        return cmd;
    }
}
