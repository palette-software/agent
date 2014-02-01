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

    protected ProcessCollection allProcesses;

    public PaletteHandler(Agent agent)
    {
        // agent autorization parameters come from the agent instance.
        this.agent = agent;
 
        //represents all running processes managed by agent
        allProcesses = new ProcessCollection();
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

        int xid = GetXid(req); // FIXME: test for XID existence.
        Dictionary<string, string> d = new Dictionary<string, string>();
        d["xid"] = Convert.ToString(xid);
        string cmd = GetCmd(req);
        Dictionary<string, object> outputBody = null;

        //TODO: put these in .ini file                
        string outputFolder = "C:\\Temp\\";  //TESTING ONLY
        string binaryFolder = "C:\\Program Files\\Tableau\\Tableau Server\\8.1\\bin\\";  //TESTING ONLY
   
        if (req.Method == "POST")
        {
            if (cmd.Contains("backup") || cmd.Contains("restore") || cmd.Contains("status"))
            {
                string[] parts = cmd.Split(' ');
                //create new process
                allProcesses.AddCLIProcess(xid, binaryFolder, outputFolder, parts[0], parts[1] + " " + parts[2]);

                outputBody = allProcesses.GetOutgoingBody(xid);
            }
            else
            {
                new HttpNotFound();
            }
        }
        else if (req.Method == "GET")
        {
            //check status of existing process        
            int test = allProcesses.GetProcessStatus(xid);
            outputBody = allProcesses.GetOutgoingBody(xid);
        }
        res.Write(fastJSON.JSON.Instance.ToJSON(outputBody));
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

    protected int GetXid(HttpRequest req)
    {
        // FIXME: test for XID existence.
        long xid = (long)req.JSON["xid"];
        return (int)xid;
    }

    protected string GetCmd(HttpRequest req)
    {
        // FIXME: test for cli existence.
        string cmd = req.JSON["cli"].ToString();
        return cmd;
    }
}
