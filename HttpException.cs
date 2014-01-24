using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class HttpException: Exception
{
    public int StatusCode = -1;
    public string Reason = null;
    public string Body = null;

    public HttpException() { }

    public HttpException(string body)
    {
        Body = body;
    }
}

class HttpBadRequest : HttpException
{
    new public int StatusCode = 400;
    new public string Reason = "Bad Request";

    public HttpBadRequest(string body) : base(body) { }
}

class HttpNotFound : HttpException
{
    new public int StatusCode = 404;
    new public string Reason = "Not Found";
}

class HttpMethodNotAllowed : HttpException
{
    new public int StatusCode = 405;
    new public string Reason = "Method Not Allowed";
}