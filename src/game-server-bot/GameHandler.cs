using System.Collections.Generic;
using GameFrame;

namespace GameServer
{
    interface GameHandler
    {
        void PrepareNewGame(Dictionary<string, string> parameters);
        void UpdateGameState(Dictionary<string, string> parameters);
        IState MakeMove(Dictionary<string, string> parameters);
    }
}
