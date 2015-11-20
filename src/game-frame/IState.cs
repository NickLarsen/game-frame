namespace GameFrame
{
    public interface IState
    {
        int ActivePlayer { get; } // TODO: make ActivePlayer a role index instead of negation based
        ulong GetStateHash();
        ushort GetHistoryHash();
        float GetHeuristicValue();
        string LastMoveDescription();
        void PreRun();
        void PostRun();
    }
}
