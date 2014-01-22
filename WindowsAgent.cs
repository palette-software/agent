using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Configuration;
using System.Diagnostics;

using Bend.Util;
using Palette;

public class Agent
{
    public static int Main(String[] args)
    {
        //MyWebServer webserv = new MyWebServer();

        ConnectToController("localhost");

        //int port = 8081;
        //Bend.Util.MyHttpServer controller = new Bend.Util.MyHttpServer(port);

        //controller.connect();

        //HttpProcessor processor = new HttpProcessor(, controller); 

        return 0;
    }

    public static void ConnectToController(string server)
    {
        try
        {
            //Set up variables and String to post to the server.
            Encoding ASCII = Encoding.ASCII;
            string Post = "Agent version 0 running on " + server + "\n";
            Byte[] BytePost = ASCII.GetBytes(Post);
            Byte[] RecvBytes = new Byte[1024];
            String strRetData = null;
            int iStartPos = 0;
            String sRequest;

            // Define those variables to be evaluated in the next for loop and 
            // then used to connect to the server. These variables are defined
            // outside the for loop to make them accessible there after.
            Socket sock = null;
            IPEndPoint hostEndPoint;
            IPAddress localAddr = IPAddress.Parse("127.0.0.1");
            int conPort = 8081;

            // Get DNS host information.
            IPHostEntry hostInfo = Dns.GetHostEntry(server);
            // Get the DNS IP addresses associated with the host.
            //IPaddress = ipHostInfo.AddressList[0];

            Console.WriteLine("Attempting to connect to host name : " + hostInfo.HostName);
            Console.WriteLine("\n With IP address : " + localAddr.ToString());

            // Creates the Socket to send data over a TCP connection.
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string connectedIP = null;

            hostEndPoint = new IPEndPoint(localAddr, conPort);

            sock.Connect(hostEndPoint);

            if (!sock.Connected)
            {
                // Connection failed, try next IPaddress.
                Console.WriteLine("Unable to connect to host " + localAddr.ToString());
                sock = null;                        
            }
            else
            {
                Console.WriteLine("Connected to host " + localAddr.ToString());
                connectedIP = localAddr.ToString();
                //break;
            }

            //Console.WriteLine("starting: " + request);  

            // Send the data to the host.
            sock.Send(BytePost, BytePost.Length, 0);

            // Receive the content and loop until all the data is received.
            Int32 bytes = sock.Receive(RecvBytes, RecvBytes.Length, 0);
            strRetData = strRetData + ASCII.GetString(RecvBytes, 0, bytes);

            //Use this only if we need to handle longer messages than 1024 bytes
            //while (bytes > 0)
            //{
            //    bytes = sock.Receive(RecvBytes, RecvBytes.Length, 0);
            //    strRetData = strRetData + ASCII.GetString(RecvBytes, 0, bytes);
            //}

            Console.WriteLine("Received from " + connectedIP.ToString() + ": " + strRetData);

            //At present we will only deal with GET type
            if (strRetData.Substring(0, 3) != "GET")
            {
                Console.WriteLine("Only Get Method is supported..");
                sock.Close();
                return;
            }
            // Look for HTTP request
            iStartPos = strRetData.IndexOf("HTTP", 1);
            // Get the HTTP text and version e.g. it will return "HTTP/1.1"
            string sHttpVersion = strRetData.Substring(iStartPos, 8);
            // Extract the Requested Type and Requested file/directory
            sRequest = strRetData.Substring(0, iStartPos - 1);
            //Replace backslash with Forward Slash, if Any
            sRequest.Replace("\\", "/");
            //If file name is not supplied add forward slash to indicate 
            //that it is a directory and then we will look for the 
            //default file name..
            if ((sRequest.IndexOf(".") < 1) && (!sRequest.EndsWith("/")))
            {
                sRequest = sRequest + "/";
            }
            //Extract the requested file name
            iStartPos = sRequest.LastIndexOf("/") + 1;
            //sRequestedFile = sRequest.Substring(iStartPos);
            //Extract The directory Name
            //sDirName = sRequest.Substring(sRequest.IndexOf("/"), sRequest.LastIndexOf("/") - 3);
            
               
            /*  

            //Put these in a config file or database for version 0
            string tableauFolder = @"C:\Program Files\Tableau\";
            string binFolder = tableauFolder + @"Tableau Server\8.1\bin\";
            string dataFolder = tableauFolder + @"Tableau Server\8.1\data\";
            string outputFolder = tableauFolder + @"Tableau Server\8.1\temp\";
            string processName = binFolder + "tabadmin";
            string jobName = "backup";
            string arguments = jobName + " " + dataFolder;  //@"backup C:\ProgramData\Tableau\Tableau Server\data\tableau_2014011401";

            SpawnProcess(binFolder, processName, jobName, arguments, dataFolder, outputFolder);
             
            */

        } // End of the try block.

        catch (SocketException e)
        {
            Console.WriteLine("SocketException caught!");
            Console.WriteLine("Source : " + e.Source);
            Console.WriteLine("Message : " + e.Message);
        }
        catch (ArgumentNullException e)
        {
            Console.WriteLine("ArgumentNullException caught!");
            Console.WriteLine("Source : " + e.Source);
            Console.WriteLine("Message : " + e.Message);
        }
        catch (NullReferenceException e)
        {
            Console.WriteLine("NullReferenceException caught!");
            Console.WriteLine("Source : " + e.Source);
            Console.WriteLine("Message : " + e.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception caught!");
            Console.WriteLine("Source : " + e.Source);
            Console.WriteLine("Message : " + e.Message);
        }

        //return strRetPage;
    }

    public static void SpawnProcess(string binFolder, string processName, string jobName, string arguments, string dataFolder, string outputFolder)
    {
        Process tableauOperation = new Process();
        tableauOperation.StartInfo.FileName = binFolder + processName;
        tableauOperation.StartInfo.Arguments = arguments + "tableau_" + jobName + "_" + System.DateTime.Now.ToString("yyyy.mm.dd");
        tableauOperation.StartInfo.UseShellExecute = false;
        tableauOperation.StartInfo.RedirectStandardOutput = true;
        tableauOperation.Start();

        string outputFileName = outputFolder + "tableau_" + jobName + "_" + System.DateTime.Now.ToString("yyyy.mm.dd") + ".out";

        Console.WriteLine(tableauOperation.StandardOutput.ReadToEnd());

        using (FileStream fs = new FileStream(outputFileName, FileMode.CreateNew))
        {
            using (BinaryWriter w = new BinaryWriter(fs))
            {
                w.Write(tableauOperation.StandardOutput.ReadToEnd());
            }
        }

        tableauOperation.WaitForExit();
    }
}

public static class UriExtensions
{
    public static Uri SetPort(this Uri uri, int newPort)
    {
        var builder = new UriBuilder(uri);
        builder.Port = newPort;
        return builder.Uri;
    }
}

