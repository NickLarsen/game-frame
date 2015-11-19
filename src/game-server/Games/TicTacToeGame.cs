using System.Collections.Generic;
using System.Linq;
using GameFrame.Games;

namespace GameServer.Games
{
    class TicTacToeGame : Game<TicTacToeState>
    {
        public TicTacToeGame(ClientConnection player1, ClientConnection player2)
            : base(player1, player2, new TicTacToeGameRules(), 1000)
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
            var moves = serverState.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Select(m => int.Parse(m))
                .ToArray();
            var state = TicTacToeState.Empty;
            foreach (var move in moves)
            {
                state.ApplyMove(move);
                state.PreRun();
            }
            return state;
        }
    }
}
