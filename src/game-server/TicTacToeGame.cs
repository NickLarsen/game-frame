using System;
using System.Collections.Generic;
using System.Linq;
using GameFrame.Games;

namespace GameServer
{
    class TicTacToeGame : Game
    {
        private readonly TicTacToeGameRules gameRules = new TicTacToeGameRules();

        public TicTacToeGame(ClientConnection player1, ClientConnection player2)
            : base(player1, player2, "tictactoe", 1000)
        {
        }

        protected override List<ClientConnection> DetermineWinners(string gameState, ClientConnection player1, ClientConnection player2)
        {
            var result = new List<ClientConnection>();
            var state = BuildState(gameState);
            var winner = gameRules.GetWinningPlayerNumber(state);
            if (winner.HasValue)
            {
                if (winner == 0)
                {
                    result.Add(player1);
                    result.Add(player2);
                }
                else
                {
                    var p = winner == 1 ? player1 : player2;
                    result.Add(p);
                }
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
    }
}
