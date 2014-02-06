using System;
using System.Collections.Generic;
using System.IO;

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

    public String QueryString = null;
    public Dictionary<string, string> QUERY = new Dictionary<string,string>();

    public HttpResponse Response = null;
    public int ContentLength = 0;

    public string ContentType = "text/plain";

    public string data = null;
    public Dictionary<string, object> JSON = null;

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
    public static HttpRequest Get(StreamReader reader)
    {
        HttpRequest req = new HttpRequest();

        try
        {
            req.ParseRequestLine(reader.ReadLine());
            req.ReadHeaders(reader);
        }
        catch (System.IO.IOException exc)
        {
            Console.WriteLine("IOException: caught while parsing http header" + exc.ToString());
        }

        if (req.ContentLength != 0)
        {
            char[] data = new char[req.ContentLength];
            reader.Read(data, 0, req.ContentLength);
            req.data = new string(data);

            if (req.ContentType == "application/json")
            {
                req.JSON = fastJSON.JSON.Instance.Parse(req.data) as Dictionary<string, object>;
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
            throw new HttpBadRequest("invalid http request line");
        }
        Method = tokens[0].ToUpper();
        Url = tokens[1];

        ProtocolVersion = tokens[2];

        if (!ProtocolVersion.Contains("1.1"))
        {
            throw new HttpBadRequest("Invalid http version.  Use version 1.1");
        }

        tokens = Url.Split('?');
        if (tokens.Length == 1)
        {
            URI = Url;
            return;
        }
        else if (tokens.Length == 2)
        {
            URI = tokens[0];
            QueryString = tokens[1];
            ParseQueryString();
            return;
        }
        else if (tokens.Length == 2 || tokens.Length > 2)
        {
            throw new HttpBadRequest("invalid query string");
        }
        URI = tokens[0];
    }

    /// <summary>
    /// Parses query string.  Populates QUERY dictionary
    /// </summary>
    private void ParseQueryString()
    {
        string[] tokens = QueryString.Split('&');
        foreach (string token in tokens) {
            string[] keyvalue = token.Split('=');
            if (keyvalue.Length == 1)
            {
                QUERY[token] = "";
            }
            else if (keyvalue.Length == 2)
            {
                QUERY[keyvalue[0]] = keyvalue[1];
            }
            else
            {
                throw new HttpBadRequest("invalid query token");
            }
        }
    }

    /// <summary>
    /// Reads the HTTP Header line
    /// </summary>
    /// <param name="reader">StreamReader</param>
    private void ReadHeaders(StreamReader reader)
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
                throw new HttpBadRequest("invalid http header line: " + line);
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
}
