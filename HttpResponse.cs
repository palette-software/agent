﻿using System;
using System.IO;
using System.Collections;

public class HttpResponse
{
    public const string VERSION = "1.1";
    public const string SERVER = "Palette/0.0";

    protected StreamWriter writer;
    public StringWriter body = new StringWriter();
    public int StatusCode = 200;
    public string StatusDescription = "OK";
    public Hashtable Headers = new Hashtable();
    public string ContentType = "text/plain";

    public HttpResponse(StreamWriter writer)
    {
        this.writer = writer;
    }

    public void Write(string s)
    {
        body.Write(s);
    }

    public void Flush()
    {
        string body = this.body.ToString();

        writer.WriteLine("HTTP/" + VERSION + " " + Convert.ToString(StatusCode) + " " + StatusDescription);
        writer.WriteLine("Server: " + SERVER);
        writer.WriteLine("Content-Length: " + Convert.ToString(body.Length));
        writer.WriteLine("");
        if (body.Length > 0)
        {
            writer.Write(body);
        }

        writer.Flush();
    }
}