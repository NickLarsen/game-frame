using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GameFrame.Games
{
    public class NineMensMorrisState : IState
    {
        public const int BoardLength = 24;
        
        public int ActivePlayer { get; set; }
        public Tuple<int, int, int> LastMove { get; set; }
        public int WhiteUnplayed { get; set; }
        public int WhiteRemaining { get; set; }
        public int BlackUnplayed { get; set; }
        public int BlackRemaining { get; set; }
        public HashSet<ulong> StatesVisited { get; set; } 
        public bool RepeatedState { get; set; }

        private ulong boardHash { get; set; }

        public ulong GetStateHash()
        {
            return boardHash;
        }

        public int GetHistoryHash()
        {
            return LastMove.Item1 << 5 | LastMove.Item2;
        }

        public float GetHeuristicValue()
        {
            var pieceAdvantage = (WhiteRemaining - BlackRemaining) * ActivePlayer / 10f;
            int adjacentCount = 0;
            for (int i = 0; i < BoardLength; i++)
            {
                int cellPlayer = GetCellPlayer(i);
                if (cellPlayer == ActivePlayer)
                {
                    adjacentCount += NineMensMorrisGameRules.Phase2MoveMap[i].Length;
                }
            }
            var movability = adjacentCount / 10000f;
            return pieceAdvantage + movability;
        }

        public int GetCellPlayer(int cell)
        {
            var cellValue = (boardHash >> (cell * 2)) & 3UL;
            int cellPlayer = cellValue == 2UL ? -1 : (int)cellValue;
            return cellPlayer;
        }

        public int GetTotalMoves()
        {
            return 18 - WhiteUnplayed - BlackUnplayed + StatesVisited.Count;
        }

        public void PreRun()
        {
            if (!InPhase1())
            {
                RepeatedState = !StatesVisited.Add(GetStateHash());
            }
        }

        public void PostRun()
        {
            RepeatedState = false;
            if (!InPhase1())
            {
                StatesVisited.Remove(GetStateHash());
            }
        }

        public static NineMensMorrisState Empty => new NineMensMorrisState()
        {
            boardHash = 0ul,
            ActivePlayer = 1,
            LastMove = Tuple.Create(-1, -1, -1),
            WhiteUnplayed = 9,
            WhiteRemaining = 9,
            BlackUnplayed = 9,
            BlackRemaining = 9,
            StatesVisited = new HashSet<ulong>(),
            RepeatedState = false,
        };

        public NineMensMorrisState ApplyMove(Tuple<int, int, int> move)
        {
            var successor = new NineMensMorrisState()
            {
                boardHash = boardHash ^ (1UL << (25 * 2)), //indicate who's turn it is outside of the board range, has to be > 24 because we're going to shift later
                ActivePlayer = -ActivePlayer,
                LastMove = move,
                WhiteUnplayed = WhiteUnplayed,
                BlackUnplayed = BlackUnplayed,
                WhiteRemaining = WhiteRemaining,
                BlackRemaining = BlackRemaining,
                StatesVisited = StatesVisited,
            };
            if (move.Item1 < 0)
            {
                if (ActivePlayer == 1) successor.WhiteUnplayed = WhiteUnplayed - 1;
                else successor.BlackUnplayed = BlackUnplayed - 1;
            }
            else
            {
                successor.boardHash &= ~(3ul << (move.Item1 * 2));
            }
            successor.boardHash |= (ActivePlayer == 1 ? 1ul : 2ul) << (move.Item2 * 2);
            if (move.Item3 >= 0)
            {
                successor.boardHash &= ~(3ul << (move.Item3 * 2));
                if (ActivePlayer == 1) successor.BlackRemaining -= 1;
                else successor.WhiteRemaining -= 1;
            }
            return successor;
        }

        public bool CompletesMill(Tuple<int, int, int> move)
        {
            var successorBoard = ApplyMove(move).boardHash;
            ulong activePlayerStones = (successorBoard >> (ActivePlayer == 1 ? 0 : 1)) & 0x555555555555UL;
            int millLocation = move.Item2 * 2;
            ulong millCheck = FastMills[millLocation++];
            bool completesMill = (millCheck & activePlayerStones) == millCheck;
            millCheck = FastMills[millLocation];
            completesMill |= (millCheck & activePlayerStones) == millCheck;
            return completesMill;
        }

        public IEnumerable<int> GetRemovableEnemies(int player)
        {
            var enemy = player * -1;
            var enemyLocations = new HashSet<int>();
            for (int i = 0; i < BoardLength; i++)
            {
                int cellPlayer = GetCellPlayer(i);
                if (cellPlayer == enemy)
                {
                    enemyLocations.Add(i);
                }
            }
            var inMills = new HashSet<int>();
            var notInMills = new HashSet<int>();
            foreach (var location in enemyLocations)
            {
                if (inMills.Contains(location)) continue;
                var isMill = false;
                foreach (var mill in Mills[location])
                {
                    isMill = mill.All(enemyLocations.Contains);
                    if (isMill)
                    {
                        foreach (var i in mill) inMills.Add(i);
                        break;
                    }
                }
                if (isMill) inMills.Add(location);
                else notInMills.Add(location);
 
            }
            return notInMills.Count > 0 ? notInMills : inMills;
        }

        public bool InPhase1()
        {
            return WhiteUnplayed + BlackUnplayed > 0;
        }

        public bool ActivePlayerPhase2()
        {
            if (ActivePlayer == 1) return WhiteUnplayed == 0 && WhiteRemaining > 3;
            return BlackUnplayed == 0 && BlackRemaining > 3;
        }

        private const string displayFormatSmall = "{0}--{1}--{2}\n|{3}-{4}-{5}|\n||{6}{7}{8}||\n{9}{10}{11} {12}{13}{14}\n||{15}{16}{17}||\n|{18}-{19}-{20}|\n{21}--{22}--{23}";
        private const string displayFormatLarge =
@"{0}----{1}----{2}
|    |    |
| {3}--{4}--{5} |
| |  |  | |
| | {6}{7}{8} | |
{9}-{10}-{11} {12}-{13}-{14}
| | {15}{16}{17} | |
| |  |  | |
| {18}--{19}--{20} |
|    |    |
{21}----{22}----{23}";
        public override string ToString()
        {
            var args = GetBoardFormatArgs().Select(m => m == 0 ? " " : (m == 1 ? "W" : "B")).ToArray();
            return string.Format(displayFormatLarge, args);
        }

        private int[] GetBoardFormatArgs()
        {
            var board = new int[BoardLength];
            for (int i = 0; i < BoardLength; i++)
            {
                board[i] = GetCellPlayer(i);
            }
            return board;
        }

        public string LastMoveDescription()
        {
            return LastMove.Item1 + "," + LastMove.Item2 + "," + LastMove.Item3;
        }

        public void WriteDebugInfo(TextWriter output)
        {
            output.WriteLine("Board: " + string.Join(", ", GetBoardFormatArgs()));
            output.WriteLine("WhiteUnplayed: " + WhiteUnplayed);
            output.WriteLine("BlackUnplayed: " + BlackUnplayed);
            output.WriteLine("ActivePlayer: " + ActivePlayer);
            output.WriteLine($"LastMove: {LastMove.Item1}, {LastMove.Item2}, {LastMove.Item3}");
            output.WriteLine("RepeatedState: " + RepeatedState);
            output.WriteLine("StatesVisited: " + string.Join(", ", StatesVisited));
        }

        private static readonly int[][][] Mills = new int[][][]
        {
            new int[][] { new int[] { 1, 2 }, new int[] { 9, 21 } } ,
            new int[][] { new int[] { 0, 2 }, new int[] { 4, 7 } },
            new int[][] { new int[] { 0, 1 }, new int[] { 14, 23 } },
            new int[][] { new int[] { 4, 5 }, new int[] { 10, 18 } },
            new int[][] { new int[] { 3, 5 }, new int[] { 1, 7 } },
            new int[][] { new int[] { 3, 4 }, new int[] { 13, 20 } },
            new int[][] { new int[] { 7, 8 }, new int[] { 11, 15 } },
            new int[][] { new int[] { 6, 8 }, new int[] { 1, 4 } },
            new int[][] { new int[] { 6, 7 }, new int[] { 12, 17 } },
            new int[][] { new int[] { 10, 11 }, new int[] { 0, 21 } },
            new int[][] { new int[] { 9, 11 }, new int[] { 3, 18 } },
            new int[][] { new int[] { 9, 10 }, new int[] { 6, 15 } },
            new int[][] { new int[] { 13, 14 }, new int[] { 8, 17 } },
            new int[][] { new int[] { 12, 14 }, new int[] { 5, 20 } },
            new int[][] { new int[] { 12, 13 }, new int[] { 2, 23 } },
            new int[][] { new int[] { 16, 17 }, new int[] { 6, 11 } },
            new int[][] { new int[] { 15, 17 }, new int[] { 19, 22 } },
            new int[][] { new int[] { 15, 16 }, new int[] { 8, 12 } },
            new int[][] { new int[] { 19, 20 }, new int[] { 3, 10 } },
            new int[][] { new int[] { 18, 20 }, new int[] { 16, 22 } },
            new int[][] { new int[] { 18, 19 }, new int[] { 5, 13 } },
            new int[][] { new int[] { 22, 23 }, new int[] { 0, 9 } },
            new int[][] { new int[] { 21, 23 }, new int[] { 16, 19 } },
            new int[][] { new int[] { 21, 22 }, new int[] { 2, 14 } },
        };

        private static readonly ulong[] FastMills = new ulong[]
        {
            1UL <<  2 | 1UL <<  4, 1UL << 18 | 1UL << 42,// { new int[] { 1, 2 }, new int[] { 9, 21 } } ,
            1UL <<  0 | 1UL <<  4, 1UL <<  8 | 1UL << 14,// { new int[] { 0, 2 }, new int[] { 4, 7 } },
            1UL <<  0 | 1UL <<  2, 1UL << 28 | 1UL << 46,// { new int[] { 0, 1 }, new int[] { 14, 23 } },
            1UL <<  8 | 1UL << 10, 1UL << 20 | 1UL << 36,// { new int[] { 4, 5 }, new int[] { 10, 18 } },
            1UL <<  6 | 1UL << 10, 1UL <<  2 | 1UL << 14,// { new int[] { 3, 5 }, new int[] { 1, 7 } },
            1UL <<  6 | 1UL <<  8, 1UL << 26 | 1UL << 40,// { new int[] { 3, 4 }, new int[] { 13, 20 } },
            1UL << 14 | 1UL << 16, 1UL << 22 | 1UL << 30,// { new int[] { 7, 8 }, new int[] { 11, 15 } },
            1UL << 12 | 1UL << 16, 1UL <<  2 | 1UL <<  8,// { new int[] { 6, 8 }, new int[] { 1, 4 } },
            1UL << 12 | 1UL << 14, 1UL << 24 | 1UL << 34,// { new int[] { 6, 7 }, new int[] { 12, 17 } },
            1UL << 20 | 1UL << 22, 1UL <<  0 | 1UL << 42,// { new int[] { 10, 11 }, new int[] { 0, 21 } },
            1UL << 18 | 1UL << 22, 1UL <<  6 | 1UL << 36,// { new int[] { 9, 11 }, new int[] { 3, 18 } },
            1UL << 18 | 1UL << 20, 1UL << 12 | 1UL << 30,// { new int[] { 9, 10 }, new int[] { 6, 15 } },
            1UL << 26 | 1UL << 28, 1UL << 16 | 1UL << 34,// { new int[] { 13, 14 }, new int[] { 8, 17 } },
            1UL << 24 | 1UL << 28, 1UL << 10 | 1UL << 40,// { new int[] { 12, 14 }, new int[] { 5, 20 } },
            1UL << 24 | 1UL << 26, 1UL <<  4 | 1UL << 46,// { new int[] { 12, 13 }, new int[] { 2, 23 } },
            1UL << 32 | 1UL << 34, 1UL << 12 | 1UL << 22,// { new int[] { 16, 17 }, new int[] { 6, 11 } },
            1UL << 30 | 1UL << 34, 1UL << 38 | 1UL << 44,// { new int[] { 15, 17 }, new int[] { 19, 22 } },
            1UL << 30 | 1UL << 32, 1UL << 16 | 1UL << 24,// { new int[] { 15, 16 }, new int[] { 8, 12 } },
            1UL << 38 | 1UL << 40, 1UL <<  6 | 1UL << 20,// { new int[] { 19, 20 }, new int[] { 3, 10 } },
            1UL << 36 | 1UL << 40, 1UL << 32 | 1UL << 44,// { new int[] { 18, 20 }, new int[] { 16, 22 } },
            1UL << 36 | 1UL << 38, 1UL << 10 | 1UL << 26,// { new int[] { 18, 19 }, new int[] { 5, 13 } },
            1UL << 44 | 1UL << 46, 1UL <<  0 | 1UL << 18,// { new int[] { 22, 23 }, new int[] { 0, 9 } },
            1UL << 42 | 1UL << 46, 1UL << 32 | 1UL << 38,// { new int[] { 21, 23 }, new int[] { 16, 19 } },
            1UL << 42 | 1UL << 44, 1UL <<  4 | 1UL << 28,// { new int[] { 21, 22 }, new int[] { 2, 14 } },
        };
    }

    public class NineMensMorrisGameRules : GameRules<NineMensMorrisState>
    {
        public override string FirstPlayerName => "White";
        public override string SecondPlayerName => "Black";

        public override List<NineMensMorrisState> Expand(NineMensMorrisState state)
        {
            if (state.InPhase1())
            {
                return ExpandPhase1(state);
            }
            if (state.ActivePlayerPhase2())
            {
                return ExpandPhase2(state);
            }
            return ExpandPhase3(state);
        }

        private List<NineMensMorrisState> ExpandPhase1(NineMensMorrisState state)
        {
            var successors = new List<NineMensMorrisState>(24);
            for (int i = 0; i < NineMensMorrisState.BoardLength; i++)
            {
                if (state.GetCellPlayer(i) != 0) continue;
                var move = Tuple.Create(-1, i, -1);
                if (state.CompletesMill(move))
                {
                    foreach (var millSuccessor in state.GetRemovableEnemies(state.ActivePlayer))
                    {
                        var millMove = Tuple.Create(-1, i, millSuccessor);
                        var successor = state.ApplyMove(millMove);
                        successors.Add(successor);
                    }
                }
                else
                {
                    var successor = state.ApplyMove(move);
                    successors.Add(successor);
                }
            }
            return successors;
        }

        private List<NineMensMorrisState> ExpandPhase2(NineMensMorrisState state)
        {
            var successors = new List<NineMensMorrisState>(16);
            for (int i = 0; i < NineMensMorrisState.BoardLength; i++)
            {
                if (state.GetCellPlayer(i) != state.ActivePlayer) continue;
                foreach (var destination in Phase2MoveMap[i])
                {
                    if (state.GetCellPlayer(destination) != 0) continue;
                    var move = Tuple.Create(i, destination, -1);
                    if (state.CompletesMill(move))
                    {
                        foreach (var millSuccessor in state.GetRemovableEnemies(state.ActivePlayer))
                        {
                            var millMove = Tuple.Create(i, destination, millSuccessor);
                            var successor = state.ApplyMove(millMove);
                            successors.Add(successor);
                        }
                    }
                    else
                    {
                        var successor = state.ApplyMove(move);
                        successors.Add(successor);
                    }
                }
            }
            return successors;
        }

        public static readonly int[][] Phase2MoveMap = new int[][]
        {
            new int[] { 1, 9 },
            new int[] { 0, 2, 4 },
            new int[] { 1, 14 },
            new int[] { 4, 10 },
            new int[] { 1, 3, 5, 7 },
            new int[] { 4, 13 },
            new int[] { 7, 11 },
            new int[] { 4, 6, 8 },
            new int[] { 7, 12 },
            new int[] { 0, 10, 21 },
            new int[] { 3, 9, 11, 18 },
            new int[] { 6, 10, 15 },
            new int[] { 8, 13, 17 },
            new int[] { 5, 12, 14, 20 },
            new int[] { 2, 13, 23 },
            new int[] { 11, 16 },
            new int[] { 15, 17, 19 },
            new int[] { 12, 16 },
            new int[] { 10, 19 },
            new int[] { 16, 18, 20, 22 },
            new int[] { 13, 19 },
            new int[] { 9, 22 },
            new int[] { 19, 21, 23 },
            new int[] { 14, 22 },
        };

        private List<NineMensMorrisState> ExpandPhase3(NineMensMorrisState state)
        {
            var successors = new List<NineMensMorrisState>(16);
            var empties = new List<int>(NineMensMorrisState.BoardLength);
            var activeStones = new List<int>(NineMensMorrisState.BoardLength);
            for (int i = 0; i < NineMensMorrisState.BoardLength; i++)
            {
                int v = state.GetCellPlayer(i);
                if (v == 0)
                {
                    empties.Add(i);
                }
                else if (v == state.ActivePlayer)
                {
                    activeStones.Add(i);
                }
            }
            foreach (var activeStone in activeStones)
            {
                foreach (var destination in empties)
                {
                    var move = Tuple.Create(activeStone, destination, -1);
                    if (state.CompletesMill(move))
                    {
                        foreach (var millSuccessor in state.GetRemovableEnemies(state.ActivePlayer))
                        {
                            var millMove = Tuple.Create(activeStone, destination, millSuccessor);
                            var successor = state.ApplyMove(millMove);
                            successors.Add(successor);
                        }
                    }
                    else
                    {
                        var successor = state.ApplyMove(move);
                        successors.Add(successor);
                    }
                }
            }
            return successors;
        }

        public override float? DetermineWinner(NineMensMorrisState state)
        {
            // TODO: if you're going to win, you want to win in shortest but if you are going to lose you want to lose in longest
            if (state.RepeatedState) return 0f;
            if (state.InPhase1()) return null;
            var movementPenalty = state.GetTotalMoves() / 10000000f;
            var importantPieces = state.ActivePlayer == 1 ? state.WhiteRemaining : state.BlackRemaining;
            if (importantPieces < 3) return -1f + movementPenalty;
            if (state.ActivePlayerPhase2() && !AnyPhase2Moves(state)) // can only lose if active player cannot move
            {
                return -1f + movementPenalty;
            }
            return null;
        }

        private bool AnyPhase2Moves(NineMensMorrisState state)
        {
            for (int i = 0; i < NineMensMorrisState.BoardLength; i++)
            {
                if (state.GetCellPlayer(i) != state.ActivePlayer) continue;
                foreach (var destination in Phase2MoveMap[i])
                {
                    if (state.GetCellPlayer(destination) == 0) return true;
                }
            }
            return false;
        }

        public override int? GetWinningPlayerNumber(NineMensMorrisState state)
        {
            var winner = DetermineWinner(state);
            if (winner == null) return null;
            if (winner == 0f) return 0;
            return state.ActivePlayer * -1;
        }
    }
}
