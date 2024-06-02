using MessagePack;

namespace Shared {
    [MessagePackObject(true)]
    public struct Cell {
        public ushort Block;
        public byte DamageLevel;

        public Cell(ushort block, byte damageLevel) {
            Block = block;
            DamageLevel = damageLevel;
        }

        public Cell(ushort idx) {
            Block = idx;
            DamageLevel = 0;
        }
    }
}