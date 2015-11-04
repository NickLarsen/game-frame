using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace GameServer
{
    class Server
    {
        private Socket listener;
        private readonly TextWriter logWriter;
        private readonly List<ClientConnection> bots = new List<ClientConnection>();

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
            Log("Server started (" + localEndPoint + "), waiting for connections...");
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
            client.Send(value);
        }

        private void HandleConnected(ClientConnection client)
        {
            Log(client.Name + " is now connected.");
            client.OnConnected -= HandleConnected;
            client.OnReceive += LogClient;
            client.OnDisconnected += HandleDisconnect;
            if (client.Name.StartsWith("bot:"))
            {
                bots.Add(client);
                if (bots.Count == 2)
                {
                    //PlayTicTacToe(bots[0], bots[1]);
                    PlayNineMensMorris(bots[0], bots[1]);
                }
            }
        }

        private void PlayTicTacToe(ClientConnection player1, ClientConnection player2)
        {
            var game = new TicTacToeGame(player1, player2);
            game.OnCompleted += GameCompletedHandler;
            Log("Starting TicTacToe Game: " + game.GetDescription());
            game.Start();
        }

        private void PlayNineMensMorris(ClientConnection player1, ClientConnection player2)
        {
            var game = new NineMensMorrisGame(player1, player2);
            game.OnCompleted += GameCompletedHandler;
            Log("Starting TicTacToe Game: " + game.GetDescription());
            game.Start();
        }

        private void GameCompletedHandler(Game game, string winner)
        {
            Log("Ending Game: " + game.GetDescription() + ", winner = " + winner);
        }

        private void HandleDisconnect(ClientConnection client, Exception ex)
        {
            Log(client.Name + " has disconnected.");
            bots.Remove(client);
        }

        private void LogClient(ClientConnection client, string value)
        {
            Log(client.Name + "> " + value);
        }

        private void Log(string value)
        {
            logWriter.WriteLine("[{0}] {1}", DateTime.UtcNow, value);
        }
    }
}
