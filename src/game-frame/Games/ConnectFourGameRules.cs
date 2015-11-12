using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameFrame.Games
{
    public class ConnectFourState : IState
    {
        public ulong Player1Moves { get; set; }
        public ulong Player2Moves { get; set; }
        public int ActivePlayer { get; set; }
        public int LastMove { get; set; }

        public ulong GetStateHash()
        {
            // TODO: check perf on this inansity
            return (ulong)(uint)Player1Moves.GetHashCode() << 32 | (ulong)(uint)Player2Moves.GetHashCode();
        }

        public uint GetHistoryHash()
        {
            return (uint)LastMove;
        }

        public float GetHeuristicValue()
        {
            // TODO: need something here
            return -0.01f;
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
            var args = GetBoard().Select((m, i) => (m == 0 ? "*" : (m == 1 ? "X" : "O")) + (i % 7 == 6 ? "\n" : "")).ToArray();
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

        public void WriteDebugInfo(TextWriter output)
        {
            output.WriteLine("Player1Moves: " + Player1Moves);
            output.WriteLine("Player2Moves: " + Player2Moves);
            output.WriteLine("ActivePlayer: " + ActivePlayer);
            output.WriteLine("LastMove: " + LastMove);
        }
    }

    public class ConnectFourGameRules : GameRules<ConnectFourState>
    {
        public override string FirstPlayerName => "W";
        public override string SecondPlayerName => "B";

        public override List<ConnectFourState> Expand(ConnectFourState state)
        {
            var all = state.Player1Moves | state.Player2Moves;
            var successors = new List<ConnectFourState>();
            for (int i = 0; i < 7; i++)
            {
                ulong col = (all >> (i * 6)) & 0x3fUL;
                int row = BitCount(col);
                if (row < 6) successors.Add(state.ApplyMove(i * 6 + row));
            }
            return successors;
        }

        private static int BitCount(ulong value)
        {
            ulong result = value - ((value >> 1) & 0x5555555555555555UL);
            result = (result & 0x3333333333333333UL) + ((result >> 2) & 0x3333333333333333UL);
            return (int)(unchecked(((result + (result >> 4)) & 0xF0F0F0F0F0F0F0FUL) * 0x101010101010101UL) >> 56);
        }

        public override float? DetermineWinner(ConnectFourState state)
        {
            if (state.LastMove == -1) return null;
            ulong lastPlayerMoves = state.ActivePlayer == 1 ? state.Player2Moves : state.Player1Moves;
            foreach (var winner in winners[state.LastMove])
            {
                if ((lastPlayerMoves & winner) == winner)
                {
                    return -1f;
                }
            }
            return null;
        }

        public override int? GetWinningPlayerNumber(ConnectFourState state)
        {
            var winner = DetermineWinner(state);
            if (winner == null) return null;
            if (winner == 0f) return 0;
            return state.ActivePlayer * -1;
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
