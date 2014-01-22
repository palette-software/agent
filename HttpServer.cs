using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Palette
{
    // MyWebServer Written by Imtiaz Alam
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    class MyWebServer
    {
        private TcpListener myListener;
        private int port = 8081; // Select any free port you wish 
        //The constructor which make the TcpListener start listening on th
        //given port. It also calls a Thread on the method StartListen(). 
        public MyWebServer()
        {
            try
            {
                //start listing on the given port
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");
                myListener = new TcpListener(localAddr, port);
                myListener.Start();
                Console.WriteLine("Web Server Running... Press ^C to Stop...");
                //start the thread which calls the method 'StartListen'
                Thread th = new Thread(new ThreadStart(StartListen));
                th.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("An Exception Occurred while Listening :" + e.ToString());
            }
        }
        public void StartListen()
        {
            int iStartPos = 0;
            String sRequest;
            String sDirName;
            String sRequestedFile;
            //String sErrorMessage;
            //String sLocalDir;
            //String sMyWebServerRoot = "C:\\MyWebServerRoot\\";
            //String sPhysicalFilePath = "";
            //String sFormattedMessage = "";
            //String sResponse = "";

            while (true)
            {
                //Accept a new connection
                Socket mySocket = myListener.AcceptSocket();
                Console.WriteLine("Socket Type " + mySocket.SocketType);
                if (mySocket.Connected)
                {
                    Console.WriteLine("\nClient Connected!!\n==================\nCLient IP {0}\n", mySocket.RemoteEndPoint);
                    //make a byte array and receive data from the client 
                    Byte[] bReceive = new Byte[1024];
                    int i = mySocket.Receive(bReceive, bReceive.Length, 0);
                    //Convert Byte to String
                    string sBuffer = Encoding.ASCII.GetString(bReceive);
                    //At present we will only deal with GET type
                    if (sBuffer.Substring(0, 3) != "GET")
                    {
                        Console.WriteLine("Only Get Method is supported..");
                        mySocket.Close();
                        return;
                    }
                    // Look for HTTP request
                    iStartPos = sBuffer.IndexOf("HTTP", 1);
                    // Get the HTTP text and version e.g. it will return "HTTP/1.1"
                    string sHttpVersion = sBuffer.Substring(iStartPos, 8);
                    // Extract the Requested Type and Requested file/directory
                    sRequest = sBuffer.Substring(0, iStartPos - 1);
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
                    sRequestedFile = sRequest.Substring(iStartPos);
                    //Extract The directory Name
                    sDirName = sRequest.Substring(sRequest.IndexOf("/"), sRequest.LastIndexOf("/") - 3);

                }
            }
        }
    }
}




