using System;
using System.Collections;

public class HttpRequest
{
    public string method;
    public string url;
    public string protocol_version;
    public Hashtable headers = new Hashtable();

    public HttpRequest()
    {
    }

    public override string ToString()
    {
        return method + " " + url;
    }
}
