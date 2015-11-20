namespace GameFrame
{
    public interface IState
    {
        int ActivePlayer { get; }
        ulong GetStateHash();
        ushort GetHistoryHash();
        float GetHeuristicValue();
        string LastMoveDescription();
        void PreRun();
        void PostRun();
    }
}
