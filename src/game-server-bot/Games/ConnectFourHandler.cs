using System;
using System.Linq;
using GameFrame.Games;

namespace GameServer.Games
{
    class ConnectFourHandler : GameHandler<ConnectFourState>
    {
        public ConnectFourHandler()
            : base(new ConnectFourGameRules())
        {
        }

        protected override ConnectFourState BuildState(string serverState)
        {
            var moves = serverState.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Select(m => int.Parse(m))
                .ToArray();
            var state = ConnectFourState.Empty;
            foreach (var move in moves)
            {
                state.ApplyMove(move);
                state.PreRun(); // updates visited states for draw check
            }
            return state;
        }
    }
}
