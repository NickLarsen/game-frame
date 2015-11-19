using System.Collections.Generic;
using System.Linq;
using GameFrame;
using GameFrame.Games;

namespace GameServer
{
    class TicTacToeHandler : GameHandler
    {
        private Player<TicTacToeState> player;
        private TicTacToeState state;

        public void PrepareNewGame(Dictionary<string, string> parameters)
        {
            var gameRules = new TicTacToeGameRules();
            var role = parameters["role"];
            var moveTime = int.Parse(parameters["milliseconds-per-move"]);
            player = new NegamaxPlayer<TicTacToeState>(gameRules, role, moveTime, 2f, null);
        }

        public void UpdateGameState(Dictionary<string, string> parameters)
        {
            state = BuildState(parameters["state"]);
        }

        private static TicTacToeState BuildState(string serverState)
        {
            var moves = serverState.ToCharArray().Select(m => int.Parse(m.ToString())).ToArray();
            var state = new TicTacToeState()
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

        public IState MakeMove(Dictionary<string, string> parameters)
        {
            return player.MakeMove(state);
        }
    }
}
