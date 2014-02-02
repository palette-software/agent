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
    public HttpBadRequest(string body)
        : base(body)
    {
        StatusCode = 400;
        Reason = "Bad Request";
    }
}

class HttpNotFound : HttpException
{
    public HttpNotFound()
        : base()
    {
        StatusCode = 404;
        Reason = "Not Found";
    }
}

class HttpMethodNotAllowed : HttpException
{
    public HttpMethodNotAllowed()
        : base()
    {
        StatusCode = 404;
        Reason = "Method Not Allowed";
    }
}