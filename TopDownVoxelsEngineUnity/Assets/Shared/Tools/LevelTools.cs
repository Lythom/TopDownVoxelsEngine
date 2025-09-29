using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Shared {
    public static class LevelTools {
        /// <summary>
        /// This function gives the cell position (in LevelData) from a world position.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns>World position of the center of a cell</returns>
        public static Vector3Int WorldToCell(this Vector3 worldPosition) {
            // cells are visually centered, so a positions between the boundaries (-0.5,-0.5,-0.5)->(0.5,0.5,0.5)
            // should snap to 0.
            return M.RoundToInt(worldPosition);
        }

        // Add high-performance versions for critical paths
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetChunkPosition(Vector3 worldPosition, out int chX, out int chZ) {
            GetChunkPosition(worldPosition.X, worldPosition.Z, out chX, out chZ);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetChunkPosition(float wx, float wz, out int chX, out int chZ) {
            int cX = M.RoundToInt(wx);
            int cZ = M.RoundToInt(wz);
            chX = M.FloorToInt(cX / (float) Chunk.Size);
            chZ = M.FloorToInt(cZ / (float) Chunk.Size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAir(this CellV0 c) {
            return c.Block == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAir(this CellV0? c) {
            return c == null || IsAir(c.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAir(this BlockId id) {
            return id == BlockId.Air;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WorldToCellInChunk(int x, int y, int z, out uint cx, out uint cy, out uint cz) {
            cx = M.Mod(x, Chunk.Size);
            cy = M.Mod(y, Chunk.Height);
            cz = M.Mod(z, Chunk.Size);
        }

        public static (BlueprintV0?, string? error) CreateBlueprint(string creatorId, string name, Vector3Int anchorPosition, Vector3Int size, LevelMap level, BlockPathMapping levelBlockPathMapping) {
            // Extract cells from the level within the blueprint area
            var cells = new CellV0[size.X, size.Y, size.Z];
            var blockMapping = new BlockPathMapping();

            // Center is at the anchor point
            var startX = anchorPosition.X - (size.X - 1) / 2;
            var startZ = anchorPosition.Z - (size.Z - 1) / 2;

            HashSet<uint> mapped = new HashSet<uint>();
            for (int x = 0; x < size.X; x++)
            for (int y = 0; y < size.Y; y++)
            for (int z = 0; z < size.Z; z++) {
                var worldPos = new Vector3Int(startX + x, anchorPosition.Y + y, startZ + z);
                var foundCell = level.TryGetExistingCell(worldPos);
                if (!foundCell.HasValue) return (null, $"Invalid cell coordinate: {worldPos}.");
                var cell = foundCell.Value;
                cells[x, y, z] = cell;
                if (mapped.Add(cell.Block)) blockMapping.BlockPathById[cell.Block] = levelBlockPathMapping.BlockPathById[cell.Block];
            }

            var blueprint = new BlueprintV0 {
                Id = Guid.NewGuid(),
                Name = name,
                CreatorId = creatorId,
                CreationDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow,
                Size = size,
                Cells = new CellArrayV0(cells),
                BlockMapping = blockMapping,
                FloorHeight = 0, // Default value, can be modified later
                PossibleSymmetries = Symmetries.None // Default value, can be modified later
            };
            return (blueprint, null);
        }
    }
}