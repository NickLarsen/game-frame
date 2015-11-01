using System.Threading.Tasks;

namespace GameFrame
{
    public abstract class Player<TState> where TState: IState
    {
        protected GameRules<TState> GameRules { get; }
        public string Name { get; protected set; }

        protected Player(GameRules<TState> gameRules)
        {
            GameRules = gameRules;
        }

        public abstract TState MakeMove(TState state);

        public virtual Task<TState> MakeMoveAsync(TState state)
        {
            return Task.Run(() => MakeMove(state));
        }
    }
}
