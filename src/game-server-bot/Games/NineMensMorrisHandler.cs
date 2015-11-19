using System;
using System.Collections.Generic;
using System.Linq;
using GameFrame;
using GameFrame.Games;

namespace GameServer.Games
{
    class NineMensMorrisHandler : GameHandler
    {
        private Player<NineMensMorrisState> player;
        private NineMensMorrisState state;

        public void PrepareNewGame(Dictionary<string, string> parameters)
        {
            var gameRules = new NineMensMorrisGameRules();
            var role = parameters["role"];
            var moveTime = int.Parse(parameters["milliseconds-per-move"]);
            player = new NegamaxPlayer<NineMensMorrisState>(gameRules, role, moveTime, 2f, null);
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
            var state = NineMensMorrisState.Empty;
            foreach (var move in moves)
            {
                state.ApplyMove(move);
                state.PreRun(); // updates visited states for draw tracking
            }
            return state;
        }

        public IState MakeMove(Dictionary<string, string> parameters)
        {
            return player.MakeMove(state);
        }
    }
}
