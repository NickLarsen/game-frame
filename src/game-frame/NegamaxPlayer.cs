using System;
using System.Collections.Generic;
using System.Linq;

namespace GameFrame
{
    public class NegamaxPlayer<TState> : Player<TState> where TState: IState
    {
        public int PlayerNumber { get; }
        public int MillisecondsPerMove { get; }
        public float HistoryPowerBase { get; }

        private Dictionary<ulong, TranspositionTableEntry> transpositionTable;
        private Dictionary<int, long> historyScores;
        private long evals;
        private int maxDepth;
        private DateTime start;
        private readonly Random random;
        private bool ignoringTimer = false;
        private readonly int maxSearchDepth;

        public NegamaxPlayer(GameRules<TState> gameRules, int playerNumber, int millisecondsPerMove, float historyPowerBase, int? randomSeed = null, int maxSearchDepth = int.MaxValue)
            : base(gameRules)
        {
            PlayerNumber = playerNumber;
            Name = playerNumber == 1 ? gameRules.FirstPlayerName : gameRules.SecondPlayerName;
            MillisecondsPerMove = millisecondsPerMove;
            HistoryPowerBase = historyPowerBase;
            random = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();
            this.maxSearchDepth = maxSearchDepth;
        }

        public override TState MakeMove(TState state)
        {
            start = DateTime.UtcNow;
            evals = 0;
            transpositionTable = new Dictionary<ulong, TranspositionTableEntry>();
            historyScores = new Dictionary<int, long>();
            List<TState> bestOverall = null;
            var possibleMoves = GameRules.Expand(state);
            int depth = 2;
            while (true)
            {
                Console.WriteLine("search depth: " + depth);
                maxDepth = depth - 1;
                float best = float.MinValue;
                List<TState> bestMoves = new List<TState>();
                var alpha = float.MinValue;
                foreach (var successor in possibleMoves)
                {
                    var timeRunning = DateTime.UtcNow - start;
                    if (!ignoringTimer && timeRunning.TotalMilliseconds > MillisecondsPerMove) break;
                    successor.PreRun();
                    var value = -Negamax(successor, depth-1, float.MinValue, -alpha);
                    successor.PostRun();
                    Console.WriteLine(successor.LastMoveDescription() + ": " + value);
                    if (value > best)
                    {
                        best = value;
                        bestMoves = new List<TState> { successor };
                    }
                    else if (value == best)
                    {
                        bestMoves.Add(successor);
                    }
                    alpha = Math.Max(alpha, value);
                }
                var failCheck = DateTime.UtcNow - start;
                if (!ignoringTimer && bestOverall != null && failCheck.TotalMilliseconds > MillisecondsPerMove) break;
                bestOverall = bestMoves;
                if (best > 0.9f)
                {
                    Console.WriteLine($"Win found for {Name}.");
                    break;
                }
                if (best < -0.9f)
                {
                    Console.WriteLine($"Loss found for {Name}.");
                    break;
                }
                if (best == 0f)
                {
                    Console.WriteLine("Tie detected.");
                    break;
                }
                depth += 2;
                if (depth > maxSearchDepth) break;
            }
            Console.WriteLine(evals);
            var selection = bestOverall[random.Next(bestOverall.Count)];
            selection.PreRun();
            return selection;
        }

        private float Negamax(TState state, int depth, float alpha, float beta)
        {
            maxDepth = Math.Min(maxDepth, depth);
            evals++;
            float alphaOriginal = alpha;
            var stateHash = state.GetStateHash();
            var ttEntry = ttLookup(stateHash);
            if (ttEntry.Type != TranspositionTableEntryType.Invalid && ttEntry.Depth >= depth)
            {
                if (ttEntry.Type == TranspositionTableEntryType.Exact)
                {
                    return ttEntry.Value;
                }
                if (ttEntry.Type == TranspositionTableEntryType.Lowerbound)
                {
                    alpha = Math.Max(alpha, ttEntry.Value);
                }
                else if (ttEntry.Type == TranspositionTableEntryType.Upperbound)
                {
                    beta = Math.Min(beta, ttEntry.Value);
                }
                if (alpha >= beta) return ttEntry.Value;
            }
            var score = GameRules.DetermineWinner(state);
            if (score.HasValue)
            {
                return score.Value;
            }
            if (depth == 0)
            {
                return state.GetHeuristicValue();
            }
            float best = float.MinValue;
            var successors = GameRules.Expand(state).OrderByDescending(GetHistoryScore);
            foreach (var successor in successors)
            {
                var timeRunning = DateTime.UtcNow - start;
                if (!ignoringTimer && timeRunning.TotalMilliseconds > MillisecondsPerMove) break;
                successor.PreRun();
                var value = -Negamax(successor, depth-1, -beta, -alpha);
                successor.PostRun();
                best = Math.Max(best, value);
                alpha = Math.Max(alpha, value);
                if (alpha >= beta)
                {
                    AddHistoryScore(successor, (long)Math.Ceiling(Math.Pow(HistoryPowerBase, depth)));
                    break;
                };
            }
            ttEntry.Value = best;
            ttEntry.Depth = depth;
            if (best <= alphaOriginal)
            {
                ttEntry.Type = TranspositionTableEntryType.Upperbound;
            }
            else if (best >= beta)
            {
                ttEntry.Type = TranspositionTableEntryType.Lowerbound;
            }
            else
            {
                ttEntry.Type = TranspositionTableEntryType.Exact;
            }
            ttStore(stateHash, ttEntry);
            return best;
        }

        private long GetHistoryScore(TState state)
        {
            var stateHash = state.GetHistoryHash();
            if (historyScores.ContainsKey(stateHash))
            {
                return historyScores[stateHash];
            }
            return int.MinValue;
        }

        private void AddHistoryScore(TState state, long value)
        {
            var stateHash = state.GetHistoryHash();
            if (historyScores.ContainsKey(stateHash))
            {
                historyScores[stateHash] += value;
            }
            else
            {
                historyScores[stateHash] = value;
            }
        }

        private TranspositionTableEntry ttLookup(ulong key)
        {
            if (transpositionTable.ContainsKey(key))
            {
                return transpositionTable[key];
            }
            return new TranspositionTableEntry()
            {
                Type = TranspositionTableEntryType.Invalid,
            };
        }

        private void ttStore(ulong key, TranspositionTableEntry entry)
        {
            transpositionTable[key] = entry;
        }

        private class TranspositionTableEntry
        {
            public TranspositionTableEntryType Type { get; set; }
            public float Value { get; set; }
            public int Depth { get; set; }
        }

        private enum TranspositionTableEntryType
        {
            Invalid,
            Exact,
            Lowerbound,
            Upperbound,
        }
    }
}
