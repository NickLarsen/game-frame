using System;
using System.Collections.Generic;
using System.Linq;
using GameFrame;
using GameFrame.Games;

namespace GameServer
{
    class NineMensMorrisHandler : GameHandler
    {
        private Player<NineMensMorrisState> player;
        private NineMensMorrisState state;

        public void PrepareNewGame(Dictionary<string, string> parameters)
        {
            var gameRules = new NineMensMorrisGameRules();
            var playerNumber = int.Parse(parameters["player-number"]);
            var moveTime = int.Parse(parameters["milliseconds-per-move"]);
            player = new NegamaxPlayer<NineMensMorrisState>(gameRules, playerNumber, moveTime, 2f, null);
        }

        public void UpdateGameState(Dictionary<string, string> parameters)
        {
            state = BuildState(parameters["state"]);
        }

        private static NineMensMorrisState BuildState(string serverState)
        {
            var moves = serverState.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Select(m => m.Split(','))
                .Select(m => Tuple.Create(int.Parse(m[0]), int.Parse(m[1]), int.Parse(m[2])))
                .ToArray();
            var state = new NineMensMorrisState()
            {
                Board = new int?[24],
                ActivePlayer = 1,
                LastMove = Tuple.Create(-1, -1, -1),
                WhiteUnplayed = 9,
                WhiteRemaining = 9,
                BlackUnplayed = 9,
                BlackRemaining = 9,
                StatesVisited = new HashSet<long>(),
                RepeatedState = false,
            };
            foreach (var move in moves)
            {
                // set the move
                state.LastMove = move;
                // alter the board and pieces info
                if (move.Item1 >= 0) state.Board[move.Item1] = null;
                state.Board[move.Item2] = state.ActivePlayer;
                if (state.InPhase1())
                {
                    if (state.ActivePlayer == 1) state.WhiteUnplayed -= 1;
                    else state.BlackUnplayed -= 1;
                }
                if (move.Item3 >= 0)
                {
                    state.Board[move.Item3] = null;
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

        public IState MakeMove(Dictionary<string, string> parameters)
        {
            return player.MakeMove(state);
        }
    }
}
