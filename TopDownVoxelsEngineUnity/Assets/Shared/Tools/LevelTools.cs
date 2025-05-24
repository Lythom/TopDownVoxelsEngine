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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int chX, int chZ) GetChunkPosition(Vector3 worldPosition) {
            return GetChunkPosition(worldPosition.X, worldPosition.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int chX, int chZ) GetChunkPosition(float wx, float wz) {
            int cX = M.RoundToInt(wx);
            int cZ = M.RoundToInt(wz);
            int chX = M.FloorToInt(cX / (float) Chunk.Size);
            int chZ = M.FloorToInt(cZ / (float) Chunk.Size);
            return (chX, chZ);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAir(this Cell c) {
            return c.Block == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAir(this Cell? c) {
            return c == null || IsAir(c.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAir(this BlockId id) {
            return id == BlockId.Air;
        }

        public static void WorldToCellInChunk(int x, int y, int z, out uint cx, out uint cy, out uint cz) {
            cx = M.Mod(x, Chunk.Size);
            cy = M.Mod(y, Chunk.Size);
            cz = M.Mod(z, Chunk.Size);
        }

    }
}