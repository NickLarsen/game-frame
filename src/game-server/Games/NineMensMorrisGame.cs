using System;
using System.Linq;
using GameFrame.Games;

namespace GameServer.Games
{
    class NineMensMorrisGame : Game<NineMensMorrisState>
    {
        public NineMensMorrisGame(ClientConnection player1, ClientConnection player2)
            : base(player1, player2, new NineMensMorrisGameRules(), 10000)
        {
        }

        protected override NineMensMorrisState BuildState(string serverState)
        {
            var moves = serverState.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Select(m => m.Split(','))
                .Select(m => Tuple.Create(int.Parse(m[0]), int.Parse(m[1]), int.Parse(m[2])))
                .ToArray();
            var state = NineMensMorrisState.Empty;
            foreach (var move in moves)
            {
                state.ApplyMove(move);
                state.PreRun(); // updates visited states for draw check
            }
            return state;
        }
    }
}
