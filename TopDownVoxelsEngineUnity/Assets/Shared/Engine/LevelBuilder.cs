using System;

namespace Shared {
    public static class LevelBuilder {
        public static void GenerateTestChunk(int chX, int chZ, string levelId, ref Chunk chunk) {
            Logr.Log($"GenerateTestChunk {chX}, {chZ}");
            var seed = GetChunkSeed(chX, chZ, levelId);
            var rng = new Random(seed);
            // Generate a new chunk
            if (chunk.Cells == null) {
                chunk.Cells = new Cell[Chunk.Size, Chunk.Size, Chunk.Size];
                foreach (var (x, y, z) in chunk.GetCellPositions()) {
                    chunk.Cells[x, y, z] = new Cell(BlockId.Air);
                }
            }

            // ... chunk generation code
            for (int x = 0; x < Chunk.Size; x++) {
                for (int z = 0; z < Chunk.Size; z++) {
                    var groundLevel = 5;
                    // Put a wall sometimes
                    var cell = chunk.Cells[x, groundLevel + 1, z];
                    double chances = 0.02;

                    cell.Block = rng.NextDouble() < chances ? BlockId.Stone : BlockId.Air;
                    chunk.Cells[x, groundLevel + 1, z] = cell;
                    if (cell.Block == BlockId.Stone) {
                        var wallTop = chunk.Cells[x, groundLevel + 2, z];
                        wallTop.Block = rng.NextDouble() < 0.9 ? BlockId.Stone : BlockId.Air;
                        chunk.Cells[x, groundLevel + 2, z] = wallTop;
                    }

                    // Ground everywhere
                    chunk.Cells[x, groundLevel, z].Block = BlockId.Grass;
                    for (int i = groundLevel - 1; i >= 0; i--) {
                        chunk.Cells[x, i, z].Block = BlockId.Dirt;
                    }
                }
            }

            chunk.IsGenerated = true;
        }

        public static int GetChunkSeed(int chX, int chZ, string levelId) {
            return 1337 + chX + 100000 * chZ + levelId.GetHashCode() * 13;
        }
    }
}