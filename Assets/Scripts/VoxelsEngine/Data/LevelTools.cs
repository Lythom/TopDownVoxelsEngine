using System.Runtime.CompilerServices;
using UnityEngine;

namespace VoxelsEngine {
    public static class LevelTools {
        /// <summary>
        /// This function gives the cell position (in LevelData) from a world position.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public static Vector3Int WorldToCell(this Vector3 worldPosition) {
            // cells are visually centered, so a positions between the boundaries (-0.5,-0.5,-0.5)->(0.5,0.5,0.5)
            // should snap to 0.
            return Vector3Int.RoundToInt(worldPosition);
        }

        public static (int chX, int chZ) GetChunkPosition(Vector3 worldPosition) {
            return GetChunkPosition(worldPosition.x, worldPosition.z);
        }

        public static (int chX, int chZ) GetChunkPosition(float wx, float wz) {
            int cX = Mathf.RoundToInt(wx);
            int cZ = Mathf.RoundToInt(wz);
            int chX = Mathf.FloorToInt(cX / (float) ChunkData.Size);
            int chZ = Mathf.FloorToInt(cZ / (float) ChunkData.Size);
            return (chX, chZ);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAir(this Cell c) {
            return c.BlockDef == BlockDefId.Air;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAir(this Cell? c) {
            return c == null || IsAir(c.Value);
        }
        
    }
}