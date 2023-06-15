using MessagePack;

namespace Shared {
    [MessagePackObject(true)]
    public struct Cell {
        public BlockId Block;
        public byte DamageLevel;

        public Cell(BlockId block, byte damageLevel) {
            Block = block;
            DamageLevel = damageLevel;
        }

        public Cell(BlockId idx) {
            Block = idx;
            DamageLevel = 0;
        }
    }
}