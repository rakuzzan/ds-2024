using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;

namespace Server;

class Program
{
    public static void StartListening(int port)
    {
        // Привязываем сокет ко всем интерфейсам на текущей машинe
        IPAddress ipAddress = IPAddress.Any;

        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

        // CREATE
        Socket listener = new Socket(
            ipAddress.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp);

        try
        {
            // BIND
            listener.Bind(localEndPoint);

            // LISTEN
            listener.Listen(10);

            List<string> history = new List<string>();

            while (true)
            {
                // ACCEPT
                Socket handler = listener.Accept();

                byte[] buf = new byte[1024];
                string data = null;
                // RECEIVE
                int bytesRec = handler.Receive(buf);

                data = Encoding.UTF8.GetString(buf, 0, bytesRec);

                Console.WriteLine("Message received: {0}", data);
                
                history.Add(data);

                // Создаем объект StringBuilder для объединения всех строк
                StringBuilder stringBuilder = new StringBuilder();

                // Объединяем все строки в одну с разделителем (например, новая строка)
                foreach (string str in history)
                {
                    stringBuilder.AppendLine(str);
                }

                // Преобразуем объединенную строку в массив байт с помощью UTF-8 кодировки
                byte[] msg = Encoding.UTF8.GetBytes(stringBuilder.ToString());

                // SEND
                handler.Send(msg);

                // RELEASE
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Invalid usage\nUsage: dotnet run <port>");
            return;
        }
        bool success = int.TryParse(args[0], out int port);
        if (!success)
        {
            Console.WriteLine("Invalid port");
            return;
        }
        StartListening(port);
    }
}