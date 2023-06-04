using System;

namespace VoxelsEngine {
    public static class LevelBuilder {
        public static void GenerateTestChunk(int chX, int chZ, string levelId, string saveId, ref ChunkData chunk) {
            var seed = GetChunkSeed(chX, chZ, levelId, saveId);
            var rng = new Random(seed);
            // Generate a new chunk
            if (chunk.Cells == null) {
                chunk.Cells = new Cell[ChunkData.Size, ChunkData.Size, ChunkData.Size];
                foreach (var (x, y, z) in chunk.GetCellPositions()) {
                    chunk.Cells[x, y, z] = new Cell(BlockDefId.Air);
                }
            }

            // ... chunk generation code
            for (int x = 0; x < ChunkData.Size; x++) {
                for (int z = 0; z < ChunkData.Size; z++) {
                    var groundLevel = 5;
                    // Put a wall sometimes
                    var cell = chunk.Cells[x, groundLevel + 1, z];
                    double chances = 0.02;

                    cell.BlockDef = rng.NextDouble() < chances ? BlockDefId.Stone : BlockDefId.Air;
                    chunk.Cells[x, groundLevel + 1, z] = cell;
                    if (cell.BlockDef == BlockDefId.Stone) {
                        var wallTop = chunk.Cells[x, groundLevel + 2, z];
                        wallTop.BlockDef = rng.NextDouble() < 0.9 ? BlockDefId.Stone : BlockDefId.Air;
                        chunk.Cells[x, groundLevel + 2, z] = wallTop;
                    }

                    // Ground everywhere
                    chunk.Cells[x, groundLevel, z].BlockDef = BlockDefId.Grass;
                    for (int i = groundLevel - 1; i >= 0; i--) {
                        chunk.Cells[x, i, z].BlockDef = BlockDefId.Dirt;
                    }
                }
            }

            chunk.IsGenerated = true;
        }

        public static int GetChunkSeed(int chX, int chZ, string levelId, string saveId) {
            return 1337 + chX + 100000 * chZ + levelId.GetHashCode() * 13 + saveId.GetHashCode() * 7;
        }
    }
}