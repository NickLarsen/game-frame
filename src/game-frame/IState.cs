using System.IO;

namespace GameFrame
{
    public interface IState
    {
        int ActivePlayer { get; set; }
        ulong GetStateHash();
        uint GetHistoryHash(); // This will only use the lower 16 bytes for indexing, so basically keep it to a ushort.
        float GetHeuristicValue(); // this function should never return 0f else finding certain ties gets much harder
        string LastMoveDescription();
        void WriteDebugInfo(TextWriter output);
        void PreRun();
        void PostRun();
    }
}
