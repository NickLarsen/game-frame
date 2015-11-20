using System.Collections.Generic;

namespace GameFrame
{
    public abstract class GameRules<TState> where TState: IState
    {
        public abstract List<TState> Expand(TState state);
        public abstract Utility CalculateUtility(TState state);
        public abstract string[] Roles { get; }
        public abstract string Name { get; }
    }
}
