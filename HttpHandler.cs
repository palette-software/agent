using System;
using System.Web;

public abstract class HttpHandler
{
    public abstract HttpResponse handle(HttpRequest req);
}
