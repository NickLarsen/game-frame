using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using GameServer.Games;

namespace GameServer
{
    class GameServerBot
    {
        private readonly string serverHostName = Dns.GetHostName();
        private readonly int port = 11873;
        private IGameHandler gameHandler;
        private GameServerConnection connection;
        private readonly string name;

        public GameServerBot(string name)
        {
            this.name = "bot:" + name;
        }

        public void Connect()
        {
            connection = new GameServerConnection(name);
            connection.OnReceive += Log;
            connection.OnReceive += HandleReceive;
            connection.Connect(serverHostName, port);
            Log("Connected, press CTRL+C to quit");
        }

        private void HandleReceive(string value)
        {
            var message = ParseMessage(value);
            switch (message["type"])
            {
                case "prepare-new-game":
                    gameHandler = GetHandler(message["game"]);
                    gameHandler.PrepareNewGame(message);
                    connection.Send("ready");
                    return;
                case "update-game-state":
                    gameHandler.UpdateGameState(message);
                    connection.Send("ready");
                    break;
                case "make-move":
                    var newState = gameHandler.MakeMove(message);
                    connection.Send("move " + newState.LastMoveDescription());
                    break;
                case "announce-results":
                    // this will be logged automatically, clean up resources
                    gameHandler = null;
                    break;
                default:
                    throw new Exception("Unknown message type received.");
            }
        }

        private IGameHandler GetHandler(string gameType)
        {
            if (gameType == "tictactoe") return new TicTacToeHandler();
            if (gameType == "ninemensmorris") return new NineMensMorrisHandler();
            if (gameType == "connectfour") return new ConnectFourHandler();
            throw new Exception("No handler for the specified game: " + gameType);
        }

        private Dictionary<string, string> ParseMessage(string config)
        {
            var args = config.Split(' ');
            if (args.Length == 0) throw new Exception("Invalid message.");
            var message = new Dictionary<string, string>();
            message["type"] = args[0];
            foreach (var arg in args.Skip(1))
            {
                var parts = arg.Split('=');
                if (parts.Length != 2) throw new Exception("Invalid message.");
                message[parts[0]] = parts[1];
            }
            return message;
        }

        private void Log(string value)
        {
            Console.WriteLine("[{0}] {1}", DateTime.UtcNow, value);
        }
    }
}
