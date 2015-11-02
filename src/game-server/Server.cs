using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace GameServer
{
    class Server
    {
        private Socket listener;
        private readonly TextWriter logWriter;

        public Server(TextWriter logWriter)
        {
            this.logWriter = logWriter;
        }

        public void Start(string hostName, int port)
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(hostName);
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);
            listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(localEndPoint);
            listener.Listen(100);
            AcceptNext();
            Console.WriteLine("Server started, waiting for connections...");
        }

        public void Stop()
        {
            listener.Shutdown(SocketShutdown.Both);
            listener.Close();
        }

        private void AcceptNext()
        {
            listener.BeginAccept(AcceptCallback, null);
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            var clientSocket = listener.EndAccept(ar);
            var client = new ClientConnection(clientSocket);
            client.OnConnected += HandleConnected;
            client.InitiateProtocolHandshake();
            AcceptNext();
        }

        private void EchoHandler(ClientConnection client, string value)
        {
            Log(client.Name + "> " + value);
            client.Send(value);
        }

        private void HandleConnected(ClientConnection client)
        {
            Log(client.Name + " is now connected.");
            client.OnConnected -= HandleConnected;
            client.OnReceive += EchoHandler;
            client.OnDisconnected += HandleDisconnect;
        }

        private void HandleDisconnect(ClientConnection client, Exception ex)
        {
            Log(client.Name + " has disconnected.");
        }

        private void Log(string value)
        {
            logWriter.WriteLine("[{0}] {1}", DateTime.UtcNow, value);
        }
    }
}
