using System.Collections.Generic;

namespace GameFrame
{
    class TranspositionTable
    {
        private Dictionary<ulong, TranspositionTableEntry> transpositionTable;

        public TranspositionTable()
        {
            Clear();
        }

        public TranspositionTableEntry Lookup(ulong key)
        {
            if (transpositionTable.ContainsKey(key))
            {
                return transpositionTable[key];
            }
            return new TranspositionTableEntry()
            {
                Type = TranspositionTableEntryType.Invalid,
            };
        }

        public void Store(ulong key, TranspositionTableEntry entry)
        {
            transpositionTable[key] = entry;
        }

        public void Clear()
        {
            transpositionTable = new Dictionary<ulong, TranspositionTableEntry>();
        }
    }

    class TranspositionTableEntry
    {
        public TranspositionTableEntryType Type { get; set; }
        public float Value { get; set; }
        public int Depth { get; set; }
    }

    enum TranspositionTableEntryType
    {
        Invalid,
        Exact,
        Lowerbound,
        Upperbound,
    }
}
