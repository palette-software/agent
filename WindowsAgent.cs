using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Configuration;
using System.Diagnostics;

using Bend.Util;

public class Agent
{
    public static int Main(String[] args)
    {
        ConnectToController("localhost");

        int port = 8081;
        Bend.Util.MyHttpServer controller = new Bend.Util.MyHttpServer(port);

        controller.connect();

        HttpProcessor processor = new HttpProcessor(, controller); 

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
            Byte[] RecvBytes = new Byte[256];
            String strRetData = null;

            // Define those variables to be evaluated in the next for loop and 
            // then used to connect to the server. These variables are defined
            // outside the for loop to make them accessible there after.
            Socket sock = null;
            IPEndPoint hostEndPoint;
            IPAddress hostAddress = null;
            int conPort = 8081;

            // Get DNS host information.
            IPHostEntry hostInfo = Dns.GetHostEntry(server);
            // Get the DNS IP addresses associated with the host.
            //IPaddress = ipHostInfo.AddressList[0];

            Console.WriteLine("Attempting to connect to host name : " + hostInfo.HostName);
            Console.WriteLine("\n With IP address List : ");
            for (int index = 0; index < hostInfo.AddressList.Length; index++)
            {
                Console.WriteLine(hostInfo.AddressList[index]);
            }

            // Evaluate the socket and receiving host IPAddress and IPEndPoint. 
            //hostAddress = hostInfo.AddressList[1];
            //hostEndPoint = new IPEndPoint(hostAddress, conPort);

            // IPAddress and IPEndPoint represent the endpoint that will receive the request.

            // Creates the Socket to send data over a TCP connection.
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string connectedIP = null;

            foreach (IPAddress ipaddress in hostInfo.AddressList)
            {
                // Connect to the host using its IPEndPoint.

                //Ignore the IPV6 Loopback address
                if (ipaddress.ToString() != "::1")
                {
                    hostAddress = ipaddress;
                    hostEndPoint = new IPEndPoint(hostAddress, conPort);

                    sock.Connect(hostEndPoint);

                    if (!sock.Connected)
                    {
                        // Connection failed, try next IPaddress.
                        Console.WriteLine("Unable to connect to host " + ipaddress.ToString());
                        sock = null;                        
                    }
                    else
                    {
                        Console.WriteLine("Connected to host " + ipaddress.ToString());
                        connectedIP = ipaddress.ToString();
                        break;
                    }
                }
            }

            

            /*

            //Stream inputStream = new BufferedStream(socket.GetStream());

            //// we probably shouldn't be using a streamwriter for all output from handlers either
            //StreamWriter outputStream = new StreamWriter(new BufferedStream(socket.GetStream()));

            //String request = streamReadLine(inputStream);
            //string[] tokens = request.Split(' ');
            //if (tokens.Length != 3)
            //{
            //    throw new Exception("invalid http request line");
            //}
            //http_method = tokens[0].ToUpper();
            //http_url = tokens[1];
            //http_protocol_versionstring = tokens[2];

            //Console.WriteLine("starting: " + request);

            // Send the data to the host.
            sock.Send(BytePost, BytePost.Length, 0);

            // Receive the content and loop until all the data is received.
            Int32 bytes = sock.Receive(RecvBytes, RecvBytes.Length, 0);
            strRetData = strRetData + ASCII.GetString(RecvBytes, 0, bytes);

            //while (bytes > 0)
            //{
            //    bytes = sock.Receive(RecvBytes, RecvBytes.Length, 0);
            //    strRetData = strRetData + ASCII.GetString(RecvBytes, 0, bytes);
            //}

            Console.WriteLine("Received from " + connectedIP.ToString() + ": " + strRetData);
  

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

