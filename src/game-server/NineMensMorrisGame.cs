using System;
using System.Collections.Generic;
using System.Linq;
using GameFrame.Games;

namespace GameServer
{
    class NineMensMorrisGame : Game
    {
        private readonly NineMensMorrisGameRules gameRules = new NineMensMorrisGameRules();

        public NineMensMorrisGame(ClientConnection player1, ClientConnection player2)
            : base(player1, player2, "ninemensmorris", 10000)
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

        private static NineMensMorrisState BuildState(string serverState)
        {
            var moves = serverState.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Select(m => m.Split(','))
                .Select(m => Tuple.Create(int.Parse(m[0]), int.Parse(m[1]), int.Parse(m[2])))
                .ToArray();
            var state = new NineMensMorrisState()
            {
                Board = new int[24],
                ActivePlayer = 1,
                LastMove = Tuple.Create(-1, -1, -1),
                WhiteUnplayed = 9,
                WhiteRemaining = 9,
                BlackUnplayed = 9,
                BlackRemaining = 9,
                StatesVisited = new HashSet<ulong>(),
                RepeatedState = false,
            };
            foreach (var move in moves)
            {
                // set the move
                state.LastMove = move;
                // alter the board and pieces info
                if (move.Item1 >= 0) state.Board[move.Item1] = 0;
                state.Board[move.Item2] = state.ActivePlayer;
                if (state.InPhase1())
                {
                    if (state.ActivePlayer == 1) state.WhiteUnplayed -= 1;
                    else state.BlackUnplayed -= 1;
                }
                if (move.Item3 >= 0)
                {
                    state.Board[move.Item3] = 0;
                    if (state.ActivePlayer == 1) state.BlackRemaining -= 1;
                    else state.WhiteRemaining -= 1;
                }
                // update the player
                state.ActivePlayer *= -1;
                // track visited states
                state.PreRun();
            }
            return state;
        }
    }
}
