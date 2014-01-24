using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Web;

public class HttpRequest
{
    public string Method;
    public string Url;
    public string URI;
    public string ProtocolVersion;
    public Hashtable Headers = new Hashtable();

    public String QueryString = null;
    public Dictionary<string, string> QUERY = new Dictionary<string,string>();

    public HttpResponse Response = null;
    public int ContentLength = 0;

    public string ContentType = "text/plain";

    public string data = null;
    public Hashtable JSON = null;

    public override string ToString()
    {
        return Method + " " + Url;
    }

    public static HttpRequest Get(StreamReader reader)
    {
        HttpRequest req = new HttpRequest();
        req.ParseRequestLine(reader.ReadLine());
        req.ReadHeaders(reader);

        if (req.ContentLength != 0)
        {
            char[] data = new char[req.ContentLength];
            reader.Read(data, 0, req.ContentLength);
            req.data = new string(data);

            if (req.ContentType == "application/json")
            {
                req.JSON = fastJSON.JSON.Instance.ToObject<Hashtable>(req.data);
            }
        }

        // FIXME: handle closed sockets and/or bad requests.
        return req;
    }

    protected void ParseRequestLine(string line)
    {
        string[] tokens = line.Split(' ');
        if (tokens.Length != 3)
        {
            throw new HttpBadRequest("invalid http request line");
        }
        Method = tokens[0].ToUpper();
        Url = tokens[1];
        ProtocolVersion = tokens[2];

        tokens = Url.Split('?');
        if (tokens.Length == 1)
        {
            URI = Url;
            return;
        }
        if (tokens.Length != 2)
        {
            throw new HttpBadRequest("invalid query string");
        }
        QueryString = tokens[1];
        ParseQueryString();
    }

    protected void ParseQueryString()
    {
        string[] tokens = QueryString.Split('&');
        foreach (string token in tokens) {
            string[] keyvalue = token.Split('&');
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

    protected void ReadHeaders(StreamReader reader)
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

            if (name == "CONTENT_LENGTH")
            {
                try
                {
                    ContentLength = Convert.ToInt32(value);
                }
                catch
                {
                    throw new HttpBadRequest("invalid CONTENT_LENGTH");
                }
            }

            if (name == "CONTENT_TYPE")
            {
                ContentType = value;
            }
        }
    }
}
