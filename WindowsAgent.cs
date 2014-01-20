using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class Agent
{
    public static void ConnectToController(string server)
    {
        //Set up variables and String to post to the server.
        Encoding ASCII = Encoding.ASCII;
        string Post = "Agent version 0 running on " + server + "\n";  
        Byte[] BytePost = ASCII.GetBytes(Post);
        Byte[] RecvBytes = new Byte[256];
        String strRetData = null;

        // IPAddress and IPEndPoint represent the endpoint that will receive the request.

        try
        {
            // Define those variables to be evaluated in the next for loop and 
            // then used to connect to the server. These variables are defined
            // outside the for loop to make them accessible there after.
            Socket sock = null;
            IPEndPoint hostEndPoint;
            IPAddress hostAddress = null;
            int conPort = 8080;

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

            // Sent the data to the host.
            sock.Send(BytePost, BytePost.Length, 0);

            // Receive the content and loop until all the data is received.
            Int32 bytes = sock.Receive(RecvBytes, RecvBytes.Length, 0);
            strRetData = strRetData + ASCII.GetString(RecvBytes, 0, bytes);

            while (bytes > 0)
            {
                bytes = sock.Receive(RecvBytes, RecvBytes.Length, 0);
                strRetData = strRetData + ASCII.GetString(RecvBytes, 0, bytes);
            }

            Console.WriteLine("Received from " + connectedIP.ToString() + ": " + strRetData);

        } // End of the try block.

        catch (SocketException e)
        {
            Console.WriteLine("SocketException caught!!!");
            Console.WriteLine("Source : " + e.Source);
            Console.WriteLine("Message : " + e.Message);
        }
        catch (ArgumentNullException e)
        {
            Console.WriteLine("ArgumentNullException caught!!!");
            Console.WriteLine("Source : " + e.Source);
            Console.WriteLine("Message : " + e.Message);
        }
        catch (NullReferenceException e)
        {
            Console.WriteLine("NullReferenceException caught!!!");
            Console.WriteLine("Source : " + e.Source);
            Console.WriteLine("Message : " + e.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception caught!!!");
            Console.WriteLine("Source : " + e.Source);
            Console.WriteLine("Message : " + e.Message);
        }

        //return strRetPage;

    }

    public static int Main(String[] args)
    {
        ConnectToController("localhost");
        
        return 0;
    }
}
