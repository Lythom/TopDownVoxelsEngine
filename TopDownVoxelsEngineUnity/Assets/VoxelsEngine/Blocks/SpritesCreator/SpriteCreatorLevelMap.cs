using Shared;


namespace VoxelsEngine {
    /// <summary>
    /// Coordinates from 0,0,0 to 15,63,15
    /// </summary>
    public class SpriteCreatorLevelMap : ILevelMap {
        private readonly Chunk[,] _chunks = new Chunk[1, 1];

        public SpriteCreatorLevelMap() {
            var c = _chunks[0, 0];
            c.Cells = new CellV0[Chunk.Size, Chunk.Height, Chunk.Size];
            foreach (var (x, y, z) in c.GetCellPositions()) {
                c.Cells![x, y, z] = new CellV0(BlockId.Air);
            }

            c.IsGenerated = true;
            _chunks[0, 0] = c;
        }

        public CellV0? GetNeighbor(int x, int y, int z, Direction dir) {
            var offset = dir.GetOffset();
            var yWithOffset = y + offset.y;
            if (yWithOffset < 0 || yWithOffset >= Chunk.Height) return null;
            return TryGetExistingCell(x + offset.x, yWithOffset, z - offset.z);
        }

        public CellV0? TryGetExistingCell(int x, int y, int z) {
            var c = _chunks[0, 0];
            if (y < 0 || y >= Chunk.Height || x < 0 || x >= Chunk.Size || z < 0 || z >= Chunk.Size || c.Cells == null) return null;
            return c.Cells[x, y, z];
        }

        public bool CellMatchDefinition(Vector3Int position, BlockId referenceBlock) {
            var c = _chunks[0, 0];
            if (position.Y < 0 || position.Y >= Chunk.Height || position.X < 0 || position.X >= Chunk.Size || position.Z < 0 || position.Z >= Chunk.Size || c.Cells == null) return referenceBlock.IsAir();
            return c.Cells[position.X, position.Y, position.Z].Block == referenceBlock;
        }

        public Chunk[,] Chunks => _chunks;

        public bool TrySetExistingCell(int x, int y, int z, BlockId block) {
            var c = _chunks[0, 0];
            if (y < 0 || y >= Chunk.Height || x < 0 || x >= Chunk.Size || z < 0 || z >= Chunk.Size || c.Cells == null) return false;
            c.Cells[x, y, z].Block = block;
            return true;
        }

        public void Clear() {
            var c = _chunks[0, 0];
            if (c.Cells == null) return;
            foreach (var (x, y, z) in c.GetCellPositions()) {
                c.Cells[x, y, z] = new CellV0(BlockId.Air);
            }
        }
    }
}