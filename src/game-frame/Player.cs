namespace GameFrame
{
    public abstract class Player<TState> where TState: IState
    {
        public string Role { get; }
        protected GameRules<TState> GameRules { get; }

        protected Player(string role, GameRules<TState> gameRules)
        {
            Role = role;
            GameRules = gameRules;
        }

        public abstract TState MakeMove(TState state);
    }
}
