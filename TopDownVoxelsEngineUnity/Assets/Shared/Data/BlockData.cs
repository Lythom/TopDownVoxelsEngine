using System;

namespace Shared
{

    [Serializable]
    public struct BlockData
    {
        public BlockId Id;

        public int MiningLevel;
        public Equipment RequiredEquipment;

        public BlockData(BlockId id, int miningLevel, Equipment requiredEquipment)
        {
            MiningLevel = miningLevel;
            RequiredEquipment = requiredEquipment;
            Id = id;
        }
    }
}