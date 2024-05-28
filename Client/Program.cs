using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client;

class Program
{
    public static void StartClient(string hostaddress, int port, string msg)
    {
        try
        {
            // Разрешение сетевых имён
            IPAddress ipAddress = (hostaddress == "localhost") ? IPAddress.Loopback : IPAddress.Parse(hostaddress);

            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

            // CREATE
            Socket sender = new Socket(
                ipAddress.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp);

            try
            {
                // CONNECT
                sender.Connect(remoteEP);

                // SEND
                int bytesSent = sender.Send(Encoding.UTF8.GetBytes(msg));

                // RECEIVE
                byte[] buf = new byte[1024];
                int bytesRec = sender.Receive(buf);

                Console.WriteLine("{0}",
                    Encoding.UTF8.GetString(buf, 0, bytesRec));

                // RELEASE
                sender.Shutdown(SocketShutdown.Both);
                sender.Close();
            }
            catch (ArgumentNullException ane)
            {
                Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
            }
            catch (SocketException se)
            {
                Console.WriteLine("SocketException : {0}", se.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.ToString());
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    static void Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Invalid usage.\nUsage: dotnet run <hostAddress> <port> <msg>");
            return;
        }
        string hostAddress = args[0];
        bool success = int.TryParse(args[1], out int port);
        string msg = args[2];

        if (String.IsNullOrEmpty(msg))
        {
            Console.WriteLine("Empty message.");
            return;
        }

        if (!success)
        {
            Console.WriteLine("Invalid port");
            return;
        }

        StartClient(hostAddress, port, msg);
    }
}