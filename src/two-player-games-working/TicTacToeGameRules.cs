using System.Collections.Generic;
using System.Linq;

namespace two_player_games_working
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
                hash <<= 2;
                hash |= hashPartLookup[Board[i] ?? 0];
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

        private static readonly Dictionary<int?, long> hashPartLookup = new Dictionary<int?, long>
        {
            { 0, 0x0L },
            { 1, 0x1L },
            { -1, 0x2L },
        };

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
    }

    public class TicTacToeGameRules : GameRules<TicTacToeState>
    {
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

        public override int? DetermineWinner(TicTacToeState state)
        {
            for (int i = 0; i < winners.Length; i++)
            {
                int[] winner = winners[i];
                int? first = state.Board[winner[0]];
                if (!first.HasValue) continue;
                if (first == state.Board[winner[1]] && first == state.Board[winner[2]])
                {
                    return first.Value;
                }
            }
            return state.Empties == 0 ? 0 : (int?)null;
        }

        static readonly int[][] winners = new int[][]
        {
            new int[] { 0,1,2 },
            new int[] { 3,4,5 },
            new int[] { 6,7,8 },
            new int[] { 0,3,6 },
            new int[] { 1,4,7 },
            new int[] { 2,5,8 },
            new int[] { 0,4,8 },
            new int[] { 2,4,6 },
        };
    }
}
