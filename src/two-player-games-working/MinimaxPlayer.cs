using System;
using System.Collections.Generic;
using System.Linq;

namespace two_player_games_working
{
    public class NegamaxPlayer<TState> : Player<TState> where TState: IState
    {
        public int PlayerNumber { get; }

        private Dictionary<long, TranspositionTableEntry> transpositionTable;
        private Dictionary<int, long> historyScores;
        private long evals;
        private int maxDepth;
        private DateTime start;
        private const int millisecondsPerMove = 900;
        private Random random = new Random();

        public NegamaxPlayer(GameRules<TState> gameRules, int playerNumber)
            : base(gameRules)
        {
            PlayerNumber = playerNumber;
        }

        public override TState MakeMove(TState state)
        {
            start = DateTime.UtcNow;
            evals = 0;
            transpositionTable = new Dictionary<long, TranspositionTableEntry>();
            historyScores = new Dictionary<int, long>();
            List<TState> bestOverall;
            var possibleMoves = GameRules.Expand(state);
            int depth = 2;
            while (true)
            {
                Console.WriteLine("search depth: " + depth);
                maxDepth = depth - 1;
                float best = float.MinValue;
                List<TState> bestMoves = new List<TState>();
                foreach (var successor in possibleMoves)
                {
                    var value = -Negamax(successor, depth-1, float.MinValue, float.MaxValue, -PlayerNumber);
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
                }
                bestOverall = bestMoves;
                if (maxDepth > 0)
                {
                    break;
                }
                depth += 2;
            }
            Console.WriteLine(evals);
            return bestOverall[random.Next(bestOverall.Count)];
        }

        private float Negamax(TState state, int depth, float alpha, float beta, int playerNumber)
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
                if (score.Value == 0) return 0;
                return score.Value * playerNumber;
            }
            if (depth == 0)
            {
                return state.GetHeuristicValue() * playerNumber;
            }
            float best = float.MinValue;
            var successors = GameRules.Expand(state).OrderByDescending(GetHistoryScore);
            foreach (var successor in successors)
            {
                var timeRunning = DateTime.UtcNow - start;
                if (timeRunning.TotalMilliseconds > millisecondsPerMove) break;
                var value = -Negamax(successor, depth-1, -beta, -alpha, -playerNumber);
                best = Math.Max(best, value);
                alpha = Math.Max(alpha, value);
                if (alpha >= beta)
                {
                    AddHistoryScore(successor, 1 << depth);
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

        private TranspositionTableEntry ttLookup(long key)
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

        private void ttStore(long key, TranspositionTableEntry entry)
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
