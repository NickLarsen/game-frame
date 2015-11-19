using System;
using System.Collections.Generic;

namespace GameFrame
{
    public class NegamaxPlayer<TState> : Player<TState> where TState: IState
    {
        public int MillisecondsPerMove { get; }
        public float HistoryPowerBase { get; }
        public string Role { get; }

        private readonly TranspositionTable transpositionTable = new TranspositionTable();
        private ulong[][] historyScores;
        private long evals;
        private DateTime start;
        private readonly Random random;
#if DEBUG
        private bool ignoringTimer = true;
#else
        private bool ignoringTimer = false;
#endif
        private readonly int maxSearchDepth;

        public NegamaxPlayer(GameRules<TState> gameRules, string role, int millisecondsPerMove, float historyPowerBase, int? randomSeed = null, int maxSearchDepth = int.MaxValue)
            : base(gameRules)
        {
            Role = role;
            MillisecondsPerMove = millisecondsPerMove;
            HistoryPowerBase = historyPowerBase;
            random = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();
            this.maxSearchDepth = maxSearchDepth;
        }

        public override TState MakeMove(TState state)
        {
            start = DateTime.UtcNow;
            evals = 0;
            transpositionTable.Clear();
            historyScores = new ulong[2][] { new ulong[ushort.MaxValue], new ulong[ushort.MaxValue] };
            TState bestOverall = default(TState);
            var possibleMoves = OrderRandom(GameRules.Expand(state));
            int depth = 2;
            while (true)
            {
                Console.WriteLine("search depth: " + depth);
                float best = float.MinValue;
                TState bestMove = default(TState);
                var alpha = float.MinValue;
                var beta = float.MaxValue;
                foreach (var successor in possibleMoves)
                {
                    long lastEvals = evals;
                    var timeRunning = DateTime.UtcNow - start;
                    if (!ignoringTimer && timeRunning.TotalMilliseconds > MillisecondsPerMove) break;
                    successor.PreRun();
                    var value = -Negamax(successor, depth-1, -beta, -alpha);
                    successor.PostRun();
                    Console.WriteLine(successor.LastMoveDescription() + ": " + value + "     negamax calls: " + (evals -  lastEvals));
                    if (value > best)
                    {
                        best = value;
                        bestMove = successor;
                    }
                    alpha = Math.Max(alpha, value);
                }
                Console.WriteLine("total negamax calls: " + evals);
                var failCheck = DateTime.UtcNow - start;
                if (!ignoringTimer && bestOverall != null && failCheck.TotalMilliseconds > MillisecondsPerMove) break;
                bestOverall = bestMove;
                if (best > 0.9f)
                {
                    Console.WriteLine($"Win found for {Role}.");
                    break;
                }
                if (best < -0.9f)
                {
                    Console.WriteLine($"Loss found for {Role}.");
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
            bestOverall.PreRun();
            return bestOverall;
        }

        private float Negamax(TState state, int depth, float alpha, float beta)
        {
            evals++;
            var score = GameRules.DetermineWinner(state);
            if (score.HasValue)
            {
                return score.Value;
            }
            if (depth == 0)
            {
                return state.GetHeuristicValue();
            }
            float alphaOriginal = alpha;
            var stateHash = state.GetStateHash();
            var ttEntry = transpositionTable.Lookup(stateHash);
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
            float best = float.MinValue;
            uint bestHistoryHash = 0U;
            var successors = OrderSuccessors(GameRules.Expand(state));
            foreach (var successor in successors)
            {
                var timeRunning = DateTime.UtcNow - start;
                if (!ignoringTimer && timeRunning.TotalMilliseconds > MillisecondsPerMove) break;
                successor.PreRun();
                var value = -Negamax(successor, depth-1, -beta, -alpha);
                successor.PostRun();
                if (value > best)
                {
                    best = value;
                    bestHistoryHash = successor.GetHistoryHash();
                }
                alpha = Math.Max(alpha, value);
                if (alpha >= beta)
                {
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
            transpositionTable.Store(stateHash, ttEntry);
            var stateHistoryValue = (ulong)Math.Ceiling(Math.Pow(HistoryPowerBase, depth));
            var playerIndex = (state.ActivePlayer + 1) / 2;
            checked { historyScores[playerIndex][bestHistoryHash] += stateHistoryValue; }
            return best;
        }

        private TState[] OrderSuccessors(List<TState> successors)
        {
            const int maxSuccessorsBits = 8; // hack for sorting history score as a single number
            const ulong maxSuccessorsBitsMask = 0xffUL; // hack for sorting history score as a single number

            var scores = new ulong[successors.Count];
            ulong j = 0;
            for (int i = 0; i < successors.Count; i++)
            {
                var successor = successors[i];
                var playerIndex = (successor.ActivePlayer + 1) / 2;
                ulong historyScore = historyScores[playerIndex][successor.GetHistoryHash() & 0xffffU];
                scores[i] = historyScore << maxSuccessorsBits | j;
                j++;
            }
            BubbleSortDesc(scores);
            var ordered = new TState[scores.Length];
            for (int i = 0; i < scores.Length; i++)
            {
                int succesorIndex = (int)(scores[i] & maxSuccessorsBitsMask);
                ordered[i] = successors[succesorIndex];
            }
            return ordered;
        }

        private TState[] OrderRandom(List<TState> successors)
        {
            const int maxSuccessorsBits = 8; // hack for sorting history score as a single number
            const ulong maxSuccessorsBitsMask = 0xffUL; // hack for sorting history score as a single number

            var scores = new ulong[successors.Count];
            ulong j = 0;
            for (int i = 0; i < successors.Count; i++)
            {
                ulong score = (ulong)random.Next() & 0xffffUL;
                scores[i] = score << maxSuccessorsBits | j;
                j++;
            }
            BubbleSortDesc(scores);
            var ordered = new TState[scores.Length];
            for (int i = 0; i < scores.Length; i++)
            {
                int succesorIndex = (int)(scores[i] & maxSuccessorsBitsMask);
                ordered[i] = successors[succesorIndex];
            }
            return ordered;
        }

        // why? it's really fast for small input sizes and requires no allocations
        private void BubbleSortDesc(ulong[] scores)
        {
            bool ordered = false;
            while (!ordered)
            {
                ordered = true;
                for (int i = 1; i < scores.Length; i++)
                {
                    if (scores[i] > scores[i - 1])
                    {
                        var t = scores[i];
                        scores[i] = scores[i - 1];
                        scores[i - 1] = t;
                        ordered = false;
                    }
                }
            }
        }
    }
}
