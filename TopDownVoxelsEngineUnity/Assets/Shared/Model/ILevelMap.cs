namespace Shared {
    public interface ILevelMap {
        Cell? GetNeighbor(int x, int y, int z, Direction dir);
        bool CellMatchDefinition(Vector3Int position, BlockId referenceBlock);
        Chunk[,] Chunks { get; }
        bool TrySetExistingCell(int x, int y, int z, BlockId block);
        void Clear();
    }
}