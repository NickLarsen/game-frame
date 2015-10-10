﻿using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GameFrame.TicTacToe
{
    public class TicTacToeState : IState
    {
        public int?[] Board { get; set; }
        public int Empties { get; set; }
        public int ActivePlayer { get; set; }
        public int LastMove { get; set; }

        public long GetStateHash()
        {
            long hash = 0;
            for (int i = 0; i < Board.Length; i++)
            {
                int? stone = Board[i];
                hash <<= 2;
                hash |= stone == 1 ? 1L : (stone == -1 ? 2L : 0L);
            }
            return hash;
        }

        public int GetHistoryHash()
        {
            return LastMove;
        }

        public float GetHeuristicValue()
        {
            return 0f;
        }

        public void PreRun() { }
        public void PostRun() { }

        public static TicTacToeState Empty => new TicTacToeState()
        {
            Board = new int?[9],
            Empties = 9,
            ActivePlayer = 1,
            LastMove = -1,
        };

        public override string ToString()
        {
            var args = Board.Select(m => m.HasValue ? (m.Value == 1 ? "X" : "O") : " ").ToArray();
            return string.Format("{0}|{1}|{2}\n-+-+-\n{3}|{4}|{5}\n-+-+-\n{6}|{7}|{8}", args);
        }

        public string LastMoveDescription()
        {
            return LastMove.ToString();
        }

        public void WriteDebugInfo(TextWriter output)
        {
            output.WriteLine("Board: " + string.Join(", ", Board));
            output.WriteLine("Empties: " + Empties);
            output.WriteLine("ActivePlayer: " + ActivePlayer);
            output.WriteLine("LastMove: " + LastMove);
        }
    }

    public class TicTacToeGameRules : GameRules<TicTacToeState>
    {
        public override string FirstPlayerName => "X";
        public override string SecondPlayerName => "O";

        public override List<TicTacToeState> Expand(TicTacToeState state)
        {
            var successors = new List<TicTacToeState>();
            for (int i = 0; i < state.Board.Length; i++)
            {
                if (state.Board[i].HasValue) continue;
                var successor = new TicTacToeState()
                {
                    Board = state.Board.ToArray(),
                    Empties = state.Empties - 1,
                    ActivePlayer = -state.ActivePlayer,
                    LastMove = i,
                };
                successor.Board[i] = state.ActivePlayer;
                successors.Add(successor);
            }
            return successors;
        }

        public override float? DetermineWinner(TicTacToeState state)
        {
            if (state.LastMove == -1) return null;
            var lastPlayer = state.Board[state.LastMove];
            foreach (var winner in winners[state.LastMove])
            {
                if (winner.All(m => state.Board[m] == lastPlayer))
                {
                    return -1f;
                }
            }
            if (state.Empties == 0) return 0f;
            return null;
        }

        static readonly Dictionary<int, int[][]> winners = new Dictionary<int, int[][]>
        {
            { 0,  new int[][] { new int[] { 1, 2 }, new int[] { 3, 6 }, new int[] { 4, 8 } } },
            { 1,  new int[][] { new int[] { 0, 2 }, new int[] { 4, 7 } } },
            { 2,  new int[][] { new int[] { 0, 1 }, new int[] { 5, 8 }, new int[] { 4, 6 } } },
            { 3,  new int[][] { new int[] { 0, 6 }, new int[] { 4, 5 } } },
            { 4,  new int[][] { new int[] { 0, 8 }, new int[] { 1, 7 }, new int[] { 3, 5 }, new int[] { 2, 6 } } },
            { 5,  new int[][] { new int[] { 3, 4 }, new int[] { 2, 8 } } },
            { 6,  new int[][] { new int[] { 0, 3 }, new int[] { 2, 4 }, new int[] { 7, 8 } } },
            { 7,  new int[][] { new int[] { 1, 4 }, new int[] { 6, 8 } } },
            { 8,  new int[][] { new int[] { 2, 5 }, new int[] { 6, 7 }, new int[] { 0, 4 } } },
        };
    }
}