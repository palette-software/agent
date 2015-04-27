using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace ssltest
{
    class Program
    {
        private static readonly int TIMEOUT = 3000;

        static int Main(string[] args)
        {
            int port = 22;
            if (args.Length == 2)
            {
                port = Convert.ToInt16(args[1]);
            } else if (args.Length != 1) 
            {
                Console.Error.WriteLine("usage: ssltest <hostname> [port]\n");
                return -1;
            }

            string host = args[0];

            try
            {

                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.ReceiveTimeout = TIMEOUT;
                socket.SendTimeout = TIMEOUT;

                /* resolve the remote address on each connection in case the IP changes. */
                IPAddress addr;

                if (!IPAddress.TryParse(host, out addr))
                {
                    IPHostEntry hostEntry = Dns.GetHostEntry(host);

                    if (hostEntry != null && hostEntry.AddressList != null
                                 && hostEntry.AddressList.Length > 0)
                    {
                        if (hostEntry.AddressList.Length == 1)
                        {
                            addr = hostEntry.AddressList[0];
                        }
                        else
                        {
                            foreach (IPAddress var in hostEntry.AddressList)
                            {
                                if (var.AddressFamily == AddressFamily.InterNetwork)
                                {
                                    addr = var;
                                    break;
                                }
                            }
                        }
                    }
                }

                IPEndPoint remoteEP = new IPEndPoint(addr, port);
                socket.Connect(remoteEP);

                SslStream stream = new SslStream(new NetworkStream(socket, true), true, CertificateValidationCallback);
                stream.AuthenticateAsClient(host);

                Console.WriteLine("Address: {0}", socket.RemoteEndPoint.ToString());
                Console.WriteLine("Cipher: {0} strength {1}", stream.CipherAlgorithm, stream.CipherStrength);
                Console.WriteLine("Hash: {0} strength {1}", stream.HashAlgorithm, stream.HashStrength);
                Console.WriteLine("Key exchange: {0} strength {1}", stream.KeyExchangeAlgorithm, stream.KeyExchangeStrength);
                Console.WriteLine("Protocol: {0}", stream.SslProtocol);

                byte[] buffer = new byte[4];
                stream.Read(buffer, 0, buffer.Length);
                string str = Encoding.ASCII.GetString(buffer.ToArray());

                if (str != "POST")
                {
                    Console.Error.WriteLine("Unexpected string '{0}'", str);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("[ERROR] Exception");
                Console.Error.WriteLine(e.ToString());
                return -1;
            }

            return 0;
        }

        static bool CertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            Console.WriteLine(certificate.ToString());
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                Console.WriteLine("SSL Policy Errors: {0}", sslPolicyErrors.ToString());
                Console.WriteLine();
            }
            return true;
        }
    }
}
