using System;
using System.Linq;
using GameFrame.Games;

namespace GameServer.Games
{
    class TicTacToeHandler : GameHandler<TicTacToeState>
    {
        public TicTacToeHandler()
            : base(new TicTacToeGameRules())
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
