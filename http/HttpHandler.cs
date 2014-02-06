using System;
using System.Web;

/// <summary>
/// Abstract class that must be instantiated to handle HttpRequests
/// </summary>
public abstract class HttpHandler
{
    public abstract HttpResponse Handle(HttpRequest req);
}
