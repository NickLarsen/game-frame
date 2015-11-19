using System.Collections.Generic;
using GameFrame;

namespace GameServer
{
    public interface IGameHandler
    {
        void PrepareNewGame(Dictionary<string, string> parameters);
        void UpdateGameState(Dictionary<string, string> parameters);
        IState MakeMove(Dictionary<string, string> parameters);
    }

    public abstract class GameHandler<TState> : IGameHandler where TState : IState
    {
        protected abstract TState BuildState(string serverState);

        private Player<TState> player;
        private TState state;
        private readonly GameRules<TState> rules; 

        protected GameHandler(GameRules<TState> rules)
        {
            this.rules = rules;
        }

        public void PrepareNewGame(Dictionary<string, string> parameters)
        {
            var role = parameters["role"];
            var moveTime = int.Parse(parameters["milliseconds-per-move"]);
            player = new NegamaxPlayer<TState>(rules, role, moveTime, 2f, null);
        }

        public void UpdateGameState(Dictionary<string, string> parameters)
        {
            state = BuildState(parameters["state"]);
        }

        public IState MakeMove(Dictionary<string, string> parameters)
        {
            return player.MakeMove(state);
        }
    }
}
