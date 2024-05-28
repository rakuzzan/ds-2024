using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Chain
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Invalid usage.\nUsage: dotnet run <listening-port> <next-host> <next-port> [true]");
                return;
            }

            if (!int.TryParse(args[0], out int listeningPort))
            {
                Console.WriteLine("Invalid listening port.");
                return;
            }

            string nextHost = args[1];

            if (!int.TryParse(args[2], out int nextPort))
            {
                Console.WriteLine("Invalid next port.");
                return;
            }

            bool isInitiator = args.Length == 4 && bool.TryParse(args[3], out bool initiatorValue) && initiatorValue;

            if (isInitiator)
            {
                await StartInitiatorProcess(listeningPort, nextHost, nextPort);
            }
            else
            {
                await StartProcess(listeningPort, nextHost, nextPort);
            }
        }

        private static async Task StartInitiatorProcess(int listeningPort, string nextHost, int nextPort)
        {
            var (receiveSocket, senderSocket) = await SetupSockets(listeningPort, nextHost, nextPort);

            if (!int.TryParse(Console.ReadLine(), out int x))
            {
                Console.WriteLine("Invalid input value");
                return;
            }

            Socket handler = receiveSocket.Accept();

            SendData(senderSocket, x);

            x = ReceiveData(handler);

            SendData(senderSocket, x);

            x = ReceiveData(handler);

            Console.WriteLine(x);

            CleanupSockets(handler, senderSocket);
        }

        private static async Task StartProcess(int listeningPort, string nextHost, int nextPort)
        {
            var (receiveSocket, senderSocket) = await SetupSockets(listeningPort, nextHost, nextPort);

            if (!int.TryParse(Console.ReadLine(), out int x))
            {
                Console.WriteLine("Invalid input value");
                return;
            }

            Socket handler = receiveSocket.Accept();

            int y = ReceiveData(handler);

            int maxNumber = Math.Max(x, y);

            SendData(senderSocket, maxNumber);

            x = ReceiveData(handler);

            SendData(senderSocket, x);

            Console.WriteLine(x);

            CleanupSockets(handler, senderSocket);
        }

        private static async Task<(Socket receiveSocket, Socket senderSocket)> SetupSockets(int listeningPort, string nextHost, int nextPort)
        {
            IPAddress receiverIpAddress = IPAddress.Any;
            IPAddress senderIpAddress = (nextHost == "localhost") ? IPAddress.Loopback : IPAddress.Parse(nextHost);

            IPEndPoint remoteEPReceiver = new(receiverIpAddress, listeningPort);
            IPEndPoint remoteEPSender = new(senderIpAddress, nextPort);

            Socket receiveSocket = new(receiverIpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Socket senderSocket = new(senderIpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            receiveSocket.Bind(remoteEPReceiver);
            receiveSocket.Listen(10);

            while (true)
            {
                try
                {
                    await senderSocket.ConnectAsync(remoteEPSender);
                    break;
                }
                catch
                {
                    await Task.Delay(1000);
                }
            }

            return (receiveSocket, senderSocket);
        }

        private static void SendData(Socket socket, int data)
        {
            byte[] buffer = BitConverter.GetBytes(data);
            socket.Send(buffer);
        }

        private static int ReceiveData(Socket socket)
        {
            byte[] buffer = new byte[1024];
            socket.Receive(buffer);
            return BitConverter.ToInt32(buffer);
        }

        private static void CleanupSockets(Socket handler, Socket senderSocket)
        {
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();

            senderSocket.Shutdown(SocketShutdown.Both);
            senderSocket.Close();
        }
    }
}