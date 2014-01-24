using System;
using System.Web;
using System.Collections.Generic;
using fastJSON;

class PaletteHandler : HttpHandler
{
    override public HttpResponse handle(HttpRequest req)
    {
        HttpResponse res = req.Response;

        switch (req.Url)
        {
            case "/auth":
                res.ContentType = "application/json";

                Dictionary<string, string> d = new Dictionary<string, string>();
                string json = fastJSON.JSON.Instance.ToJSON(d);
                res.Write(json);
                break;
            case "/status":
                break;
            default:
                throw new HttpNotFound();
        }

        return res;
    }
}
