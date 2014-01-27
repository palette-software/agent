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
        req.ContentType = "application/json";

        int xid = GetXid(req.URI);
        Dictionary<string, string> d = new Dictionary<string, string>();

        if (req.Method == "PUT") 
        {
            //create new process
            //TODO: put these in .ini file
            string binaryfolder = @"C:\Program Files\Tableau\Tableau Server\8.1\bin\";
            string outputfolder = @"C:\ProgramData\Tableau\Tableau Server\data\";

            allProcesses.AddCLIProcess(xid, binaryfolder, outputfolder, GetCmd(req.URI), "");

            d["status"] = allProcesses.GetProcessStatus(xid).ToString();
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
            case "/backup":
            case "/restore":
            case "/sql":
                return HandleCmd(req);
            case "/status":
                return HandleStatus(req);
            default:
                throw new HttpNotFound();
        }
    }

    //Making the assumption here that this is the last word in the req.data
    protected int GetXid(string data)
    {
        try
        {
            Match match = Regex.Match(data, @"xid=([0-9\-]+)\$", RegexOptions.IgnoreCase);
            int xid = Convert.ToInt32(match.Groups[1].Value);
            return xid;
        }
        catch
        {
            Console.WriteLine("Error: Bad query string in command!");
            return -1;
        }
    }

    protected string GetCmd(string data)
    {
        string cmdStr = null;
        try
        {            
            if (data.Contains(@"/cli"))
            {
                cmdStr = "cli";
            }
            else if (data.Contains(@"/backup"))
            {
                cmdStr = "backup";
            }
            else if (data.Contains(@"/restore"))
            {
                cmdStr = "restore";
            }
            else if (data.Contains(@"/copy"))
            {
                cmdStr = "copy";
            }
            else if (data.Contains(@"/sql"))
            {
                cmdStr = "sql";
            }
            else
            {
                Console.WriteLine("Error: Bad query string in command!");
            }
        }
        catch
        {
            Console.WriteLine("Error: Bad query string in command!");
        }

        return cmdStr;
    }
}
