using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.ServiceModel.Web;

interface IAgentReply
{
    void SerializeToJSON(); //This serializes the object into JSON
    string ToString();
}

[DataContract]
internal class AuthReply : IAgentReply
{
    [DataMember]
    internal string domain;

    [DataMember]
    internal string username;

    [DataMember]
    internal string password;

    [DataMember]
    internal string agentversion;

    [DataMember]
    internal string ipaddress;

    [DataMember]
    internal string hostname;

    [DataMember]
    internal string listenport;

    MemoryStream stream1;

    public AuthReply(string domain, string username, string password,
        string agentversion, string ipaddress, string hostname, string listenport)
    {
        this.domain = domain;
        this.username = username;
        this.password = password;
        this.agentversion = agentversion;
        this.ipaddress = ipaddress;
        this.hostname = hostname;
        this.listenport = listenport;
    }

    public void SerializeToJSON()
    {
        MemoryStream stream = new MemoryStream();
        DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(AuthReply));

        ser.WriteObject(stream, this);

        stream1 = stream;
    }

    public override string ToString()
    {
        stream1.Position = 0;
        StreamReader sr = new StreamReader(stream1);
        return sr.ReadToEnd();
    }
}

