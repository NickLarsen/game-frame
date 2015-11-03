using System;
using System.Collections.Generic;
using System.Linq;
using GameFrame.Games;

namespace GameServer
{
    internal delegate void OnCompletedHandler(TicTacToeGame game, string winner);

    class TicTacToeGame
    {
        private readonly ClientConnection player1;
        private readonly ClientConnection player2;
        private List<string> moves;
        private bool player1Ready = false;
        private bool player2Ready = false;
        private Action readyAction;
        private string currentStatus;
        private readonly TicTacToeGameRules gameRules = new TicTacToeGameRules();

        public TicTacToeGame(ClientConnection player1, ClientConnection player2)
        {
            this.player1 = player1;
            this.player2 = player2;
        }

        public string GetDescription()
        {
            return player1.Name + " vs " + player2.Name;
        }

        public void Start()
        {
            PrepareNewGame();
        }

        private void PrepareNewGame()
        {
            moves = new List<string>();
            WhenAllReady(UpdateGameState);
            player1.Send("prepare-new-game game=tictactoe player-number=1 milliseconds-per-move=1000");
            player2.Send("prepare-new-game game=tictactoe player-number=-1 milliseconds-per-move=1000");
        }

        private void UpdateGameState()
        {
            var gameState = GenerateWireState();
            if (DetermineWinners(gameState).Any())
            {
                WhenAllReady(AnnounceWinner);
            }
            else
            {
                WhenAllReady(GameLoop);
            }
            player1.Send("update-game-state state=" + gameState);
            player2.Send("update-game-state state=" + gameState);
        }

        private string GenerateWireState()
        {
            return string.Join("", moves);
        }

        private List<ClientConnection> DetermineWinners(string gameState)
        {
            var result = new List<ClientConnection>();
            var state = BuildState(gameState);
            var winner = gameRules.DetermineWinner(state);
            if (winner.HasValue)
            {
                if (winner == 0f)
                {
                    result.Add(player1);
                    result.Add(player2);
                }
                if (winner < 0f) result.Add(player1);
                if (winner > 0f) result.Add(player2);
            }
            return result;
        }

        private static TicTacToeState BuildState(string serverState)
        {
            var moves = serverState.ToCharArray().Select(m => int.Parse(m.ToString())).ToArray();
            TicTacToeState state = new TicTacToeState()
            {
                Board = new int?[9],
                Empties = 9 - moves.Length,
                ActivePlayer = moves.Length % 2 == 0 ? 1 : -1,
                LastMove = moves.Length == 0 ? -1 : moves.Last(),
            };
            var playerNumber = 1;
            foreach (var move in moves)
            {
                state.Board[move] = playerNumber;
                playerNumber *= -1;
            }
            return state;
        }

        private void GameLoop()
        {
            var playerToMove = moves.Count % 2 == 0 ? player1 : player2;
            playerToMove.OnReceive += MakeMove;
            playerToMove.Send("make-move");
        }

        private void MakeMove(ClientConnection client, string value)
        {
            var args = value.Split(' ');
            if (args.Length != 2 || args[0] != "move")
            {
                throw new Exception("Invalid message: expected move.");
            }
            client.OnReceive -= MakeMove;
            moves.Add(args[1]);
            UpdateGameState();
        }

        private void AnnounceWinner()
        {
            var gameState = GenerateWireState();
            var winners = DetermineWinners(gameState);
            var winner = winners.Count == 1 ? winners[0].Name : "draw";
            player1.Send("announce-winner winner=" + winner);
            player2.Send("announce-winner winner=" + winner);
            if (OnCompleted != null)
            {
                OnCompleted(this, winner);
            }
        }

        private void WhenAllReady(Action nextAction)
        {
            currentStatus = "waiting";
            readyAction = nextAction;
            player1Ready = false;
            player2Ready = false;
            player1.OnReceive += ReadyListener;
            player2.OnReceive += ReadyListener;
        }

        private void ReadyListener(ClientConnection client, string value)
        {
            if (client == player1 && value == "ready")
            {
                player1Ready = true;
                player1.OnReceive -= ReadyListener;
            }
            else if (client == player2 && value == "ready")
            {
                player2Ready = true;
                player2.OnReceive -= ReadyListener;
            }
            else
            {
                throw new Exception("Invalid message: expected 'ready'.");
            }
            lock (currentStatus)
            {
                if (player1Ready && player2Ready && currentStatus == "waiting")
                {
                    currentStatus = "acting";
                    readyAction();
                }
            }
        }

        public event OnCompletedHandler OnCompleted;
    }
}
