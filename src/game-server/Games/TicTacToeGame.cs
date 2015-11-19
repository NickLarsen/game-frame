using System;
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

        protected override TicTacToeState BuildState(string serverState)
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
