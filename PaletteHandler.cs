using System;
using System.Web;
using System.Collections.Generic;
using fastJSON;

class PaletteHandler : HttpHandler
{
    protected string uuid = null;

    public PaletteHandler(string uuid)
    {
        this.uuid = uuid;
    }

    protected HttpResponse handleAuth(HttpRequest req)
    {
        HttpResponse res = req.Response;
        req.ContentType = "application/json";

        Dictionary<string, string> d = new Dictionary<string, string>();
        d["version"] = Agent.VERSION;
        d["uuid"] = uuid;

        res.Write(fastJSON.JSON.Instance.ToJSON(d));
        return res;
    }

    protected HttpResponse handleStatus(HttpRequest req)
    {
        return req.Response;
    }

    override public HttpResponse handle(HttpRequest req)
    {
        switch (req.URI)
        {
            case "/auth":
                return handleAuth(req);
            case "/status":
                return handleStatus(req);
            default:
                throw new HttpNotFound();
        }
    }
}
