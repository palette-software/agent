using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using log4net;
using log4net.Config;

/// <summary>
/// Encapsulates an HTTP request object.  
/// </summary>
public class HttpRequest
{
    public string Method;
    public string Url;
    public string URI;
    public string ProtocolVersion;
    public Dictionary<string, string> Headers = new Dictionary<string, string>();

    public String QueryString;
    public NameValueCollection QUERY;

    public HttpResponse Response = null;
    public int ContentLength = 0;

    public string ContentType = "text/plain";

    public byte[] data;
    public Dictionary<string, object> JSON = null;
    //This has to be put in each class for logging purposes
    private static readonly log4net.ILog logger = log4net.LogManager.GetLogger
    (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    /// Overrides System.ToString()
    /// </summary>
    /// <returns>Method + URL</returns>
    public override string ToString()
    {
        return Method + " " + Url;
    }

    /// <summary>
    /// Returns new HttpRequest object for "GET".  Parses JSON if ContentType == "application/json" 
    /// </summary>
    /// <param name="reader"></param>
    /// <returns></returns>
    public static HttpRequest Get(HttpStreamReader reader)
    {
        HttpRequest req = new HttpRequest();

        try
        {
            string line = reader.ReadLine();
            if (line == null || line.Length == 0)
            {
                // This prevents mis-leading exceptions when the controller gracefully closes the socket.
                logger.Error("The first line of the HTTP request was null");
                return null;
            }
            req.ParseRequestLine(line);
            req.ReadHeaders(reader);
        }
        catch (System.IO.IOException exc)
        {
            logger.Error("IOException: caught while parsing HTTP headers - " + exc.ToString());
            return null;
        }

        if (req.ContentLength != 0)
        {
            if (req.ContentType == "text/json" || req.ContentType == "application/json")
            {
                string data = reader.ReadText(req.ContentLength); 
                req.JSON = fastJSON.JSON.Instance.Parse(data) as Dictionary<string, object>;
            } else {
                req.data = reader.Read(req.ContentLength);
            }
        }

        // FIXME: handle closed sockets and/or bad requests.
        return req;
    }

    /// <summary>
    /// Parses request to get URI, Method, Protocol version, Query string 
    /// </summary>
    /// <param name="line">request string</param>
    private void ParseRequestLine(string line)
    {
        string[] tokens = line.Split(' ');
        if (tokens.Length != 3)
        {
            throw new HttpBadRequest("invalid HTTP request line");
        }
        Method = tokens[0].ToUpper();
        Url = tokens[1];

        ProtocolVersion = tokens[2];

        if (ProtocolVersion != "HTTP/1.1")
        {
            throw new HttpBadRequest("Invalid HTTP version.  Use version 1.1");
        }

        tokens = Url.Split('?');
        if (tokens.Length == 1)
        {
            URI = Url;
            QueryString = "";
            QUERY = new NameValueCollection();
            return;
        }
        else if (tokens.Length == 2)
        {
            URI = tokens[0];
            QueryString = tokens[1];
            QUERY = HttpUtility.ParseQueryString(QueryString);
            return;
        }
        else if (tokens.Length == 2 || tokens.Length > 2)
        {
            throw new HttpBadRequest("invalid query string");
        }
        URI = tokens[0];
    }

    /// <summary>
    /// Reads the HTTP Header line
    /// </summary>
    /// <param name="reader">HttpStreamReader</param>
    private void ReadHeaders(HttpStreamReader reader)
    {
        String line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Equals(""))
            {
                return;
            }

            int separator = line.IndexOf(':');
            if (separator == -1)
            {
                throw new HttpBadRequest("invalid HTTP header line: " + line);
            }
            String name = line.Substring(0, separator);
            int pos = separator + 1;
            while ((pos < line.Length) && (line[pos] == ' '))
            {
                pos++; // strip any spaces
            }

            name = name.ToUpper();
            string value = line.Substring(pos, line.Length - pos);
            Headers[name] = value;

            if (name == "CONTENT-LENGTH")
            {
                try
                {
                    ContentLength = Convert.ToInt32(value);
                }
                catch
                {
                    throw new HttpBadRequest("invalid CONTENT-LENGTH");
                }
            }

            if (name == "CONTENT-TYPE")
            {
                ContentType = value;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="?"></param>
    /// <param name="?"></param>
    /// <returns></returns>
    public object GetKeyAsObject(string name, bool required)
    {
        if (JSON == null)
        {
            if (required)
            {
                throw new HttpBadRequest("Valid JSON required.");
            }
            else
            {
                return null;
            }
        }

        if (!JSON.ContainsKey(name))
        {
            if (required)
            {
                throw new HttpBadRequest(String.Format("Missing JSON key '{0}'", name));
            }
            else
            {
                return null;
            }
        }

        return JSON[name];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public string GetKey(string name, bool required)
    {
        object value = GetKeyAsObject(name, required);
        try
        {
            return (string)value;
        }
        catch (Exception)
        {
            throw new HttpBadRequest(String.Format("Expected a string value for JSON key '{0}'", name));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public int GetKeyAsInt(string name)
    {
        object value = GetKeyAsObject(name, true);
        try
        {
            return (int)value;
        }
        catch (Exception)
        {
            throw new HttpBadRequest(String.Format("Expected an integer value for JSON key '{0}'", name));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public int GetKeyAsInt(string name, int defaultValue)
    {
        object value = GetKeyAsObject(name, false);
        if (value == null)
        {
            return defaultValue;
        }
        try
        {
            return (int)value;
        }
        catch (Exception)
        {
            throw new HttpBadRequest(String.Format("Expected an integer value for JSON key '{0}'", name));
        }
    }
}
