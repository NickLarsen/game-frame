using System;

namespace GameFrame
{
    public class UtilimaxPlayer<TState> : Player<TState> where TState : IState
    {
        public int MillisecondsPerMove { get; }
        
        private long evals;
        private DateTime start;
#if DEBUG
        private bool ignoringTimer = true;
#else
        private bool ignoringTimer = false;
#endif
        private readonly int maxSearchDepth;

        public UtilimaxPlayer(GameRules<TState> gameRules, string role, int millisecondsPerMove, int maxSearchDepth = int.MaxValue)
            : base(role, gameRules)
        {
            MillisecondsPerMove = millisecondsPerMove;
            this.maxSearchDepth = maxSearchDepth;
        }

        public override TState MakeMove(TState state)
        {
            start = DateTime.UtcNow;
            evals = 0;
            TState bestOverall = default(TState);
            var possibleMoves = GameRules.Expand(state);
            int depth = 2;
            while (true)
            {
                Console.WriteLine("search depth: " + depth);
                var best = Utility.Min(GameRules.Roles, state.ActivePlayerIndex);
                TState bestMove = default(TState);
                foreach (var successor in possibleMoves)
                {
                    long lastEvals = evals;
                    var timeRunning = DateTime.UtcNow - start;
                    if (!ignoringTimer && timeRunning.TotalMilliseconds > MillisecondsPerMove) break;
                    successor.PreRun();
                    var value = Utilimax(successor, depth - 1);
                    successor.PostRun();
                    Console.WriteLine(successor.LastMoveDescription() + ": " + value + "     utilimax calls: " + (evals - lastEvals));
                    if (value.BetterThan(best, state.ActivePlayerIndex))
                    {
                        best = value;
                        bestMove = successor;
                    }
                }
                Console.WriteLine("total utilimax calls: " + evals);
                var failCheck = DateTime.UtcNow - start;
                if (!ignoringTimer && bestOverall != null && failCheck.TotalMilliseconds > MillisecondsPerMove) break;
                bestOverall = bestMove;
                if (best.IsTerminal)
                {
                    Console.WriteLine($"Terminal Utility found: {best}");
                    break;
                }
                depth += 2;
                if (depth > maxSearchDepth) break;
            }
            bestOverall.PreRun();
            return bestOverall;
        }

        private Utility Utilimax(TState state, int depth)
        {
            evals++;
            var utility = GameRules.CalculateUtility(state);
            if (depth == 0 || utility.IsTerminal)
            {
                return utility;
            }
            var best = Utility.Min(GameRules.Roles, state.ActivePlayerIndex);
            var successors = GameRules.Expand(state);
            foreach (var successor in successors)
            {
                var timeRunning = DateTime.UtcNow - start;
                if (!ignoringTimer && timeRunning.TotalMilliseconds > MillisecondsPerMove) break;
                successor.PreRun();
                var value = Utilimax(successor, depth - 1);
                successor.PostRun();
                if (value.BetterThan(best, state.ActivePlayerIndex))
                {
                    best = value;
                }
            }
            return best;
        }
    }
}
