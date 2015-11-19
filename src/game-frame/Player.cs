namespace GameFrame
{
    public abstract class Player<TState> where TState: IState
    {
        protected GameRules<TState> GameRules { get; }

        protected Player(GameRules<TState> gameRules)
        {
            GameRules = gameRules;
        }

        public abstract TState MakeMove(TState state);
    }
}
