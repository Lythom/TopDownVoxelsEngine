namespace Shared
{
    public struct Cell
    {
        public BlockId Block;
        public byte DamageLevel;

        public Cell(BlockId idx)
        {
            Block = idx;
            DamageLevel = 0;
        }
    }
}