using System;
using System.Web;
using System.Collections.Generic;
using fastJSON;
using System.Text.RegularExpressions;

class PaletteHandler : HttpHandler
{
    protected string username = null;
    protected string password = null;
    protected string version = null;
    protected string hostname = null;
    protected string type = null;
    protected string ip_address = null;
    protected int listen_port = -1;
    protected string uuid = null;

    protected ProcessCollection allProcesses;

    public PaletteHandler(string uuid, string username, string password, string hostname, 
        string ip_address, int listen_port)
    {
        //Agent authorization parameters
        this.username = username;
        this.password = password;
        this.hostname = hostname;
        this.type = "primary";
        this.ip_address = ip_address;
        this.listen_port = listen_port;        
        this.uuid = uuid;
        //represents all running processes managed by agent
        allProcesses = new ProcessCollection();
    }

    protected HttpResponse HandleAuth(HttpRequest req)
    {
        HttpResponse res = req.Response;
        req.ContentType = "application/json";

        Dictionary<string, object> data = new Dictionary<string, object>();
        data["username"] = username;
        data["password"] = password;
        data["version"] = Agent.VERSION;
        data["hostname"] = hostname;
        data["type"] = type;
        data["ip-address"] = ip_address;
        data["listen-port"] = listen_port;
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
            outputBody = allProcesses.GetOutgoingBody(xid);
        }
        res.Write(fastJSON.JSON.Instance.ToJSON(outputBody));
        return res;
    }

    protected HttpResponse HandleStatus(HttpRequest req)
    {
        return req.Response;
    }

    override public HttpResponse Handle(HttpRequest req)
    {
        switch (req.URI)
        {
            case "/auth":
                return HandleAuth(req);
            case "/cli":
                return HandleCmd(req);
            case "/status":
                return HandleStatus(req);
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
