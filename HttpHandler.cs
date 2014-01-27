using System;
using System.Web;

public abstract class HttpHandler
{
    public abstract HttpResponse Handle(HttpRequest req);
}
