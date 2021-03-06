﻿using System.Collections.Generic;
using System.Linq;
using GameFrame.Helpers;

namespace GameFrame.Games
{
    public class ConnectFourState : IState
    {
        public ulong Player1Moves { get; set; }
        public ulong Player2Moves { get; set; }
        public int ActivePlayer { get; set; }
        public int ActivePlayerIndex { get; set; } // TODO: implement this
        public int LastMove { get; set; }

        public ulong GetStateHash()
        {
            // TODO: check perf on this inansity
            return (ulong)(uint)Player1Moves.GetHashCode() << 32 | (ulong)(uint)Player2Moves.GetHashCode();
        }

        public ushort GetHistoryHash()
        {
            return (ushort)LastMove;
        }

        public float GetHeuristicValue()
        {
            // TODO: need something here
            return 0.01f;
        }

        public void PreRun() { }
        public void PostRun() { }

        public static ConnectFourState Empty => new ConnectFourState()
        {
            Player1Moves = 0UL,
            Player2Moves = 0UL,
            ActivePlayer = 1,
            LastMove = -1,
        };

        public ConnectFourState ApplyMove(int move)
        {
            var successor = new ConnectFourState
            {
                Player1Moves = Player1Moves,
                Player2Moves = Player2Moves,
                ActivePlayer = -ActivePlayer,
                LastMove = move,
            };
            if (ActivePlayer == 1) successor.Player1Moves |= 1UL << move;
            else successor.Player2Moves |= 1UL << move;
            return successor;
        }

        public override string ToString()
        {
            var args = GetBoard().Select((m, i) => (m == 0 ? "*" : (m == 1 ? "R" : "B")) + (i % 7 == 6 ? "\n" : "")).ToArray();
            return string.Join("", args);
        }

        private int[] GetBoard()
        {
            int[] board = new int[42];
            for (int i = 0; i < 42; i++)
            {
                int col = i % 7;
                int row = i / 7;
                ulong loc = 1UL << (col * 6 + row);
                if ((Player1Moves & loc) == loc) board[41 - i] = 1;
                else if ((Player2Moves & loc) == loc) board[41 - i] = -1;
            }
            return board;
        }

        public string LastMoveDescription()
        {
            return LastMove.ToString();
        }
    }

    public class ConnectFourGameRules : GameRules<ConnectFourState>
    {
        public override string Name { get; } = "connectfour";
        public override string[] Roles { get; } = { "Red", "Black" };

        public override List<ConnectFourState> Expand(ConnectFourState state)
        {
            var all = state.Player1Moves | state.Player2Moves;
            var successors = new List<ConnectFourState>();
            for (int i = 0; i < 7; i++)
            {
                ulong col = (all >> (i * 6)) & 0x3fUL;
                int row = BitTwiddling.BitCount(col);
                if (row < 6) successors.Add(state.ApplyMove(i * 6 + row));
            }
            return successors;
        }

        public override Utility CalculateUtility(ConnectFourState state)
        {
            var utility = new Utility(Roles);
            if (state.LastMove == -1) return utility;
            ulong lastPlayerMoves = state.ActivePlayer == 1 ? state.Player2Moves : state.Player1Moves;
            foreach (var winner in winners[state.LastMove])
            {
                if ((lastPlayerMoves & winner) != winner) continue;
                utility.IsTerminal = true;
                utility[state.ActivePlayer == 1 ? 0 : 1] = -1f;
                utility[state.ActivePlayer == 1 ? 1 : 0] = 1f;
                break;
            }
            ulong empties = (state.Player1Moves | state.Player2Moves) ^ 0x3ffffffffffUL;
            if (empties == 0U) utility.IsTerminal = true;
            return utility;
        }

        static ulong[][] BuildWinners(int rows, int cols)
        {
            var winnerSets = new HashSet<ulong>[rows * cols];
            for (int i = 0; i < rows * cols; i++)
            {
                winnerSets[i] = new HashSet<ulong>();
            }
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int c2, c3, c4, c1 = col * 6 + row;

                    // down
                    if (row >= 3)
                    {
                        c2 = (col - 0) * 6 + (row - 1);
                        c3 = (col - 0) * 6 + (row - 2);
                        c4 = (col - 0) * 6 + (row - 3);
                        var winner = 1UL << c1;
                        winner |= 1UL << c2;
                        winner |= 1UL << c3;
                        winner |= 1UL << c4;
                        winnerSets[c1].Add(winner);
                        winnerSets[c2].Add(winner);
                        winnerSets[c3].Add(winner);
                        winnerSets[c4].Add(winner);
                    }
                    // right
                    if (col >= 3)
                    {
                        c2 = (col - 1) * 6 + (row - 0);
                        c3 = (col - 2) * 6 + (row - 0);
                        c4 = (col - 3) * 6 + (row - 0);
                        var winner = 1UL << c1;
                        winner |= 1UL << c2;
                        winner |= 1UL << c3;
                        winner |= 1UL << c4;
                        winnerSets[c1].Add(winner);
                        winnerSets[c2].Add(winner);
                        winnerSets[c3].Add(winner);
                        winnerSets[c4].Add(winner);
                    }
                    // right up
                    if (col >= 3 && row <= 2)
                    {
                        c2 = (col - 1) * 6 + (row + 1);
                        c3 = (col - 2) * 6 + (row + 2);
                        c4 = (col - 3) * 6 + (row + 3);
                        var winner = 1UL << c1;
                        winner |= 1UL << c2;
                        winner |= 1UL << c3;
                        winner |= 1UL << c4;
                        winnerSets[c1].Add(winner);
                        winnerSets[c2].Add(winner);
                        winnerSets[c3].Add(winner);
                        winnerSets[c4].Add(winner);
                    }
                    // left up
                    if (col <= 2 && row <= 2)
                    {
                        c2 = (col + 1) * 6 + (row - 1);
                        c3 = (col + 2) * 6 + (row - 2);
                        c4 = (col + 3) * 6 + (row - 3);
                        var winner = 1UL << c1;
                        winner |= 1UL << c2;
                        winner |= 1UL << c3;
                        winner |= 1UL << c4;
                        winnerSets[c1].Add(winner);
                        winnerSets[c2].Add(winner);
                        winnerSets[c3].Add(winner);
                        winnerSets[c4].Add(winner);
                    }
                }
            }
            return winnerSets.Select(m => m.ToArray()).ToArray();
        }
        static readonly ulong[][] winners = BuildWinners(6, 7);
    }
}
