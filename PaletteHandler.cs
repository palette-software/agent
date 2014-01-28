using System;
using System.Web;
using System.Collections.Generic;
using fastJSON;
using System.Text.RegularExpressions;

class PaletteHandler : HttpHandler
{
    protected string uuid = null;
    protected ProcessCollection allProcesses;

    public PaletteHandler(string uuid)
    {
        this.uuid = uuid;
        //represents all running processes managed by agent
        allProcesses = new ProcessCollection();
    }

    protected HttpResponse HandleAuth(HttpRequest req)
    {
        HttpResponse res = req.Response;
        req.ContentType = "application/json";

        Dictionary<string, string> d = new Dictionary<string, string>();
        d["version"] = Agent.VERSION;
        d["uuid"] = uuid;

        res.Write(fastJSON.JSON.Instance.ToJSON(d));
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

        //TODO: put these in .ini file                
        string outputFolder = "C:\\Temp\\";  //TESTING ONLY
        string binaryFolder = "C:\\Temp\\"; //"C:\\Program Files\\Tableau\\Tableau Server\\8.1\\bin\\";  //TESTING ONLY
   
        if (req.Method == "POST")
        {
            if (cmd.Contains("backup") || cmd.Contains("restore") || cmd.Contains("status"))
            {
                string[] parts = cmd.Split(' ');
                //create new process
                allProcesses.AddCLIProcess(xid, binaryFolder, outputFolder, parts[0], parts[1] + " " + parts[2]);

                d["status"] = allProcesses.GetProcessStatus(xid).ToString();
            }
            else
            {
                new HttpNotFound();
            }
        }
        else if (req.Method == "GET")
        {
            //check status of existing process        
            d["status"] = allProcesses.GetProcessStatus(xid).ToString();
        }
        res.Write(fastJSON.JSON.Instance.ToJSON(d));
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
