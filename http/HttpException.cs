using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Encapsulates an HttpException.  Inherits from System.Exception
/// </summary>
class HttpException: Exception
{
    public int StatusCode = -1;
    public string Reason = null;
    public string Body = null;

    public HttpException() { }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="body">Exception text</param>
    public HttpException(string body)
    {
        Body = body;
    }
}

/// <summary>
/// HttpBadRequest exception (status code 400)
/// </summary>
class HttpBadRequest : HttpException
{
    public HttpBadRequest(string body) : base(body)
    {
        StatusCode = 400;
        Reason = "Bad Request";
    }
}

/// <summary>
/// HttpNotFound exception (status code 404)
/// </summary>
class HttpNotFound : HttpException
{
    public HttpNotFound() : base()
    {
        StatusCode = 404;
        Reason = "Not Found";
    }
}

/// <summary>
/// HttpMethodNotAllowed exception (status code 404)
/// </summary>
class HttpMethodNotAllowed : HttpException
{
    public HttpMethodNotAllowed(): base()
    {
        StatusCode = 404;
        Reason = "Method Not Allowed";
    }
}

class HttpInternalServerError : HttpException
{
    public HttpInternalServerError(string body) : base(body)
    {
        StatusCode = 500;
        Reason = "Internal Server Error";
    }
}