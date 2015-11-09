using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GameFrame.Games
{
    public class NineMensMorrisState : IState
    {
        public int[] Board { get; set; }
        public int ActivePlayer { get; set; }
        public Tuple<int, int, int> LastMove { get; set; }
        public int WhiteUnplayed { get; set; }
        public int WhiteRemaining { get; set; }
        public int BlackUnplayed { get; set; }
        public int BlackRemaining { get; set; }
        public HashSet<long> StatesVisited { get; set; } 
        public bool RepeatedState { get; set; }

        public long GetStateHash()
        {
            long hash = 0;
            for (int i = 0; i < Board.Length; i++)
            {
                hash <<= 2;
                hash |= Board[i] & 3;
            }
            hash *= ActivePlayer;
            return hash;
        }

        public int GetHistoryHash()
        {
            return LastMove.Item1 << 5 | LastMove.Item2;
        }

        public float GetHeuristicValue()
        {
            var pieceAdvantage = (WhiteRemaining - BlackRemaining) * ActivePlayer / 10f;
            int adjacentCount = 0;
            for (int i = 0; i < Board.Length; i++)
            {
                if (Board[i] == ActivePlayer)
                {
                    adjacentCount += NineMensMorrisGameRules.Phase2MoveMap[i].Length;
                }
            }
            var movability = adjacentCount / 10000f;
            return pieceAdvantage + movability;
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
            Board = new int[24],
            ActivePlayer = 1,
            LastMove = Tuple.Create(-1, -1, -1),
            WhiteUnplayed = 9,
            WhiteRemaining = 9,
            BlackUnplayed = 9,
            BlackRemaining = 9,
            StatesVisited = new HashSet<long>(),
            RepeatedState = false,
        };

        public NineMensMorrisState ApplyMove(Tuple<int, int, int> move)
        {
            var successor = new NineMensMorrisState()
            {
                Board = (int[])Board.Clone(),
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
                successor.Board[move.Item1] = 0;
            }
            successor.Board[move.Item2] = ActivePlayer;
            if (move.Item3 >= 0)
            {
                successor.Board[move.Item3] = 0;
                if (ActivePlayer == 1) successor.BlackRemaining -= 1;
                else successor.WhiteRemaining -= 1;
            }
            return successor;
        }

        public bool CompletesMill(Tuple<int, int, int> move)
        {
            var successorBoard = ApplyMove(move).Board;
            int lastPosition = move.Item2;
            var millChecks = Mills[lastPosition];
            foreach (var millCheck in millChecks)
            {
                var completedMill = millCheck.All(m => successorBoard[m] == ActivePlayer);
                if (completedMill) return true;
            }
            return false;
        }

        public IEnumerable<int> GetRemovableEnemies(int player)
        {
            var enemy = player * -1;
            var enemyLocations = new HashSet<int>();
            for (int i = 0; i < Board.Length; i++)
            {
                if (Board[i] == enemy)
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
            if (ActivePlayer == 1) return WhiteRemaining > 3;
            return BlackRemaining > 3;
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
            var args = Board.Select(m => m == 0 ? " " : (m == 1 ? "W" : "B")).ToArray();
            return string.Format(displayFormatLarge, args);
        }

        public string LastMoveDescription()
        {
            return LastMove.Item1 + "," + LastMove.Item2 + "," + LastMove.Item3;
        }

        public void WriteDebugInfo(TextWriter output)
        {
            output.WriteLine("Board: " + string.Join(", ", Board));
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
    }

    public class NineMensMorrisGameRules : GameRules<NineMensMorrisState>
    {
        public override string FirstPlayerName => "White";
        public override string SecondPlayerName => "Black";

        public override List<NineMensMorrisState> Expand(NineMensMorrisState state)
        {
            return ExpandInternal(state).ToList();
        }

        private IEnumerable<NineMensMorrisState> ExpandInternal(NineMensMorrisState state)
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

        private IEnumerable<NineMensMorrisState> ExpandPhase1(NineMensMorrisState state)
        {
            for (int i = 0; i < state.Board.Length; i++)
            {
                if (state.Board[i] != 0) continue;
                var move = Tuple.Create(-1, i, -1);
                if (state.CompletesMill(move))
                {
                    foreach (var millSuccessor in state.GetRemovableEnemies(state.ActivePlayer))
                    {
                        var millMove = Tuple.Create(-1, i, millSuccessor);
                        var successor = state.ApplyMove(millMove);
                        yield return successor;
                    }
                }
                else
                {
                    var successor = state.ApplyMove(move);
                    yield return successor;
                }
            }
        }

        private IEnumerable<NineMensMorrisState> ExpandPhase2(NineMensMorrisState state)
        {
            var holes = state.Board.Length;
            var activeStones = new List<int>(holes);
            for (int i = 0; i < holes; i++)
            {
                int v = state.Board[i];
                if (v != 0)
                {
                    if (v == state.ActivePlayer)
                    {
                        activeStones.Add(i);
                    }
                }
            }
            foreach (var activeStone in activeStones)
            {
                foreach (var destination in Phase2MoveMap[activeStone])
                {
                    if (state.Board[destination] != 0) continue;
                    var move = Tuple.Create(activeStone, destination, -1);
                    if (state.CompletesMill(move))
                    {
                        foreach (var millSuccessor in state.GetRemovableEnemies(state.ActivePlayer))
                        {
                            var millMove = Tuple.Create(activeStone, destination, millSuccessor);
                            var successor = state.ApplyMove(millMove);
                            yield return successor;
                        }
                    }
                    else
                    {
                        var successor = state.ApplyMove(move);
                        yield return successor;
                    }
                }
            }
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

        private IEnumerable<NineMensMorrisState> ExpandPhase3(NineMensMorrisState state)
        {
            var holes = state.Board.Length;
            var empties = new List<int>(holes);
            var activeStones = new List<int>(holes);
            for (int i = 0; i < holes; i++)
            {
                int v = state.Board[i];
                if (v != 0)
                {
                    if (v == state.ActivePlayer)
                    {
                        activeStones.Add(i);
                    }
                }
                else
                {
                    empties.Add(i);
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
                            yield return successor;
                        }
                    }
                    else
                    {
                        var successor = state.ApplyMove(move);
                        yield return successor;
                    }
                }
            }
        }

        public override float? DetermineWinner(NineMensMorrisState state)
        {
            // TODO: if you're going to win, you want to win in shortest but if you are going to lose you want to lose in longest
            if (state.RepeatedState) return 0f;
            var movementPenalty = state.GetTotalMoves() / 10000000f;
            var importantPieces = state.ActivePlayer == 1 ? state.WhiteRemaining : state.BlackRemaining;
            if (importantPieces < 3) return -1f + movementPenalty;
            if (state.ActivePlayerPhase2() && !ExpandInternal(state).Any()) // can only lose if active player cannot move
            {
                return -1f + movementPenalty;
            }
            return null;
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
