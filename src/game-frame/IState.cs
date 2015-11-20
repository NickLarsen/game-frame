namespace GameFrame
{
    public interface IState
    {
        int ActivePlayer { get; set; }
        ulong GetStateHash();
        ushort GetHistoryHash();
        float GetHeuristicValue(); // this function should never return 0f else finding certain ties gets much harder
        string LastMoveDescription();
        void PreRun();
        void PostRun();
    }
}
