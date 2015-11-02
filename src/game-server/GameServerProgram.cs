using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace GameServer
{
    // https://msdn.microsoft.com/en-us/library/fx6588te(v=vs.110).aspx
    static class GameServerProgram
    {
        static readonly ManualResetEvent acceptClient = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            try
            {
                StartListening();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();
        }

        static void StartListening()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11873);
            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(localEndPoint);
            listener.Listen(100);
            Console.WriteLine("Server started, waiting for connections...");

            while (true)
            {
                acceptClient.Reset();
                listener.BeginAccept(AcceptCallback, listener);
                acceptClient.WaitOne();
            }
        }

        static void AcceptCallback(IAsyncResult ar)
        {
            acceptClient.Set();
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            var state = new ClientConnection(handler);
            state.OnConnected += HandleConnected;
            state.InitiateHandshake();
        }

        static void EchoHandler(ClientConnection client, string value)
        {
            Log(client.Name + "> " + value);
            client.Send(value);
        }

        static void HandleConnected(ClientConnection client)
        {
            Log(client.Name + " is now connected.");
            client.OnConnected -= HandleConnected;
            client.OnReceive += EchoHandler;
            client.OnDisconnected += HandleDisconnect;
        }

        static void HandleDisconnect(ClientConnection client, Exception ex)
        {
            Log(client.Name + " has disconnected.");
        }

        static void Log(string value)
        {
            Console.WriteLine("[{0}] {1}", DateTime.UtcNow, value);
        }
    }
}
