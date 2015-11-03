using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GameFrame.Games
{
    public class NineMensMorrisState : IState
    {
        public int?[] Board { get; set; }
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
                int? stone = Board[i];
                hash <<= 2;
                hash |= stone == 1 ? 1L : (stone == -1 ? 2L : 0L);
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
            var args = Board.Select(m => m.HasValue ? (m.Value == 1 ? "W" : "B") : " ").ToArray();
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
                if (state.Board[i].HasValue) continue;
                var successor = new NineMensMorrisState()
                {
                    Board = (int?[])state.Board.Clone(),
                    ActivePlayer = -state.ActivePlayer,
                    LastMove = Tuple.Create(-1, i, -1),
                    WhiteUnplayed = state.ActivePlayer == 1 ? Math.Max(0, state.WhiteUnplayed - 1) : state.WhiteUnplayed,
                    BlackUnplayed = state.ActivePlayer == -1 ? Math.Max(0, state.BlackUnplayed - 1) : state.BlackUnplayed,
                    WhiteRemaining = state.WhiteRemaining,
                    BlackRemaining = state.BlackRemaining,
                    StatesVisited = state.StatesVisited,
                };
                successor.Board[i] = state.ActivePlayer;
                foreach (var millSuccessor in ExpandMill(successor))
                {
                    yield return millSuccessor;
                }
            }
        }

        private IEnumerable<NineMensMorrisState> ExpandMill(NineMensMorrisState state)
        {
            if (!MillCompleted(state))
            {
                yield return state;
                yield break;
            }
            var removableEnemies = GetRemovableEnemies(state);
            foreach (var removableEnemy in removableEnemies)
            {
                var successor = new NineMensMorrisState()
                {
                    Board = (int?[])state.Board.Clone(),
                    ActivePlayer = state.ActivePlayer,
                    LastMove = Tuple.Create(state.LastMove.Item1, state.LastMove.Item2, removableEnemy),
                    WhiteUnplayed = state.WhiteUnplayed,
                    BlackUnplayed = state.BlackUnplayed,
                    WhiteRemaining = state.WhiteRemaining,
                    BlackRemaining = state.BlackRemaining,
                    StatesVisited = state.StatesVisited,
                };
                if (successor.ActivePlayer == 1)
                {
                    successor.WhiteRemaining -= 1;
                }
                else
                {
                    successor.BlackRemaining -= 1;
                }
                successor.Board[removableEnemy] = null;
                yield return successor;
            }
        }

        private bool MillCompleted(NineMensMorrisState state)
        {
            int lastPosition = state.LastMove.Item2;
            var player = state.Board[lastPosition].Value;
            var millChecks = Mills[lastPosition];
            foreach (var millCheck in millChecks)
            {
                var completedMill = millCheck.All(m => state.Board[m] == player);
                if (completedMill) return true;
            }
            return false;
        }

        private HashSet<int> GetRemovableEnemies(NineMensMorrisState state)
        {
            var enemy = state.Board[state.LastMove.Item2] * -1;
            var enemyLocations = new HashSet<int>();
            for (int i = 0; i < state.Board.Length; i++)
            {
                if (state.Board[i] == enemy)
                {
                    enemyLocations.Add(i);
                }
            }
            var inMills = new HashSet<int>();
            var notInMills = new HashSet<int>();
            foreach (var location in enemyLocations)
            {
                if (inMills.Contains(location)) continue;
                foreach (var mill in Mills[location])
                {
                    var isMill = mill.All(enemyLocations.Contains);
                    if (isMill)
                    {
                        inMills.Add(location);
                        foreach (var i in mill) inMills.Add(i);
                    }
                    else
                    {
                        notInMills.Add(location);
                    }
                }
            }
            return notInMills.Count > 0 ? notInMills : inMills;
        }

        private IEnumerable<NineMensMorrisState> ExpandPhase2(NineMensMorrisState state)
        {
            var holes = state.Board.Length;
            var empties = new HashSet<int>();
            var activeStones = new List<int>(holes);
            for (int i = 0; i < holes; i++)
            {
                int? v = state.Board[i];
                if (v.HasValue)
                {
                    if (v.Value == state.ActivePlayer)
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
                foreach (var destination in Phase2MoveMap[activeStone])
                {
                    if (!empties.Contains(destination)) continue;
                    var successor = new NineMensMorrisState()
                    {
                        Board = (int?[])state.Board.Clone(),
                        ActivePlayer = -state.ActivePlayer,
                        LastMove = Tuple.Create(activeStone, destination, -1),
                        WhiteUnplayed = 0,
                        BlackUnplayed = 0,
                        WhiteRemaining = state.WhiteRemaining,
                        BlackRemaining = state.BlackRemaining,
                        StatesVisited = state.StatesVisited,
                    };
                    successor.Board[activeStone] = null;
                    successor.Board[destination] = state.ActivePlayer;
                    foreach (var millSuccessor in ExpandMill(successor))
                    {
                        yield return millSuccessor;
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

        public static readonly int[][][] Mills = new int[][][]
        {
            new int[][] { new int[] { 1, 2 }, new int[] { 9, 21 } } ,
            new int[][] { new int[] { 0, 2 }, new int[] { 4, 7 } },
            new int[][] { new int[] { 0, 1 }, new int[] { 14, 23 } },
            new int[][] { new int[] { 4, 5 }, new int[] { 10, 18 } },
            new int[][] { new int[] { 3, 5 }, new int[] { 1, 7 } },
            new int[][] { new int[] { 3, 4 }, new int[] { 13, 20 } },
            new int[][] { new int[] { 7, 8 }, new int[] { 11, 15 } },
            new int[][] { new int[] { 1, 4 }, new int[] { 6, 8 } },
            new int[][] { new int[] { 6, 7 }, new int[] { 12, 17 } },
            new int[][] { new int[] { 0, 21 }, new int[] { 10, 11 } },
            new int[][] { new int[] { 3, 18 }, new int[] { 9, 11 } },
            new int[][] { new int[] { 6, 15 }, new int[] { 9, 10 } },
            new int[][] { new int[] { 8, 17 }, new int[] { 13, 14 } },
            new int[][] { new int[] { 5, 20 }, new int[] { 12, 14 } },
            new int[][] { new int[] { 2, 23 }, new int[] { 12, 13 } },
            new int[][] { new int[] { 6, 11 }, new int[] { 16, 17 } },
            new int[][] { new int[] { 15, 17 }, new int[] { 19, 22 } },
            new int[][] { new int[] { 15, 16 }, new int[] { 8, 12 } },
            new int[][] { new int[] { 3, 10 }, new int[] { 19, 20 } },
            new int[][] { new int[] { 18, 20 }, new int[] { 16, 22 } },
            new int[][] { new int[] { 18, 19 }, new int[] { 5, 13 } },
            new int[][] { new int[] { 0, 9 }, new int[] { 22, 23 } },
            new int[][] { new int[] { 16, 19 }, new int[] { 21, 23 } },
            new int[][] { new int[] { 21, 22 }, new int[] { 2, 14 } },
        };

        private IEnumerable<NineMensMorrisState> ExpandPhase3(NineMensMorrisState state)
        {
            var holes = state.Board.Length;
            var empties = new List<int>(holes);
            var activeStones = new List<int>(holes);
            for (int i = 0; i < holes; i++)
            {
                int? v = state.Board[i];
                if (v.HasValue)
                {
                    if (v.Value == state.ActivePlayer)
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
                    var successor = new NineMensMorrisState()
                    {
                        Board = (int?[])state.Board.Clone(),
                        ActivePlayer = -state.ActivePlayer,
                        LastMove = Tuple.Create(activeStone, destination, -1),
                        WhiteUnplayed = 0,
                        BlackUnplayed = 0,
                        WhiteRemaining = state.WhiteRemaining,
                        BlackRemaining = state.BlackRemaining,
                        StatesVisited = state.StatesVisited,
                    };
                    successor.Board[activeStone] = null;
                    successor.Board[destination] = state.ActivePlayer;
                    foreach (var millSuccessor in ExpandMill(successor))
                    {
                        yield return millSuccessor;
                    }
                }
            }
        }

        public override float? DetermineWinner(NineMensMorrisState state)
        {
            if (state.RepeatedState) return 0f;
            var movementPenalty = state.GetTotalMoves() / 10000000f;
            var importantPieces = state.ActivePlayer == 1 ? state.BlackRemaining : state.WhiteRemaining;
            if (importantPieces < 3) return -1f + movementPenalty;
            if (state.ActivePlayerPhase2() && !ExpandInternal(state).Any()) // can only lose if active player cannot move
            {
                return -1f + movementPenalty;
            }
            return null;
        }
    }
}
