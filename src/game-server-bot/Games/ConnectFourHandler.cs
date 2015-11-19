using System;
using System.Collections.Generic;
using System.Linq;
using GameFrame;
using GameFrame.Games;

namespace GameServer.Games
{
    class ConnectFourHandler : GameHandler
    {
        private Player<ConnectFourState> player;
        private ConnectFourState state;

        public void PrepareNewGame(Dictionary<string, string> parameters)
        {
            var gameRules = new ConnectFourGameRules();
            var role = parameters["role"];
            var moveTime = int.Parse(parameters["milliseconds-per-move"]);
            player = new NegamaxPlayer<ConnectFourState>(gameRules, role, moveTime, 2f, null);
        }

        public void UpdateGameState(Dictionary<string, string> parameters)
        {
            state = BuildState(parameters["state"]);
        }

        private static ConnectFourState BuildState(string serverState)
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

        public IState MakeMove(Dictionary<string, string> parameters)
        {
            return player.MakeMove(state);
        }
    }
}
