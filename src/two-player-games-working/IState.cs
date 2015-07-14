using System.IO;

namespace two_player_games_working
{
    public interface IState
    {
        int ActivePlayer { get; set; }
        long GetStateHash();
        int GetHistoryHash();
        float GetHeuristicValue();
        string LastMoveDescription();
        void WriteDebugInfo(TextWriter output);
        void PreRun();
        void PostRun();
    }
}
