using System.IO;

namespace GameFrame
{
    public interface IState
    {
        int ActivePlayer { get; set; }
        ulong GetStateHash();
        int GetHistoryHash();
        float GetHeuristicValue(); // this function should never return 0f else finding certain ties gets much harder
        string LastMoveDescription();
        void WriteDebugInfo(TextWriter output);
        void PreRun();
        void PostRun();
    }
}
