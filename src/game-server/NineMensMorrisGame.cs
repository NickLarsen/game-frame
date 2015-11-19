﻿using System;
using System.Collections.Generic;
using System.Linq;
using GameFrame.Games;

namespace GameServer
{
    class NineMensMorrisGame : Game<NineMensMorrisState>
    {
        public NineMensMorrisGame(ClientConnection player1, ClientConnection player2)
            : base(player1, player2, new NineMensMorrisGameRules(), 10000)
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
