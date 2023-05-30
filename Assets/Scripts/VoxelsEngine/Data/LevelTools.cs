using UnityEngine;

namespace VoxelsEngine {
    public static class LevelTools {
        public static Vector3Int Snapped(this Vector3 worldPosition) {
            return Vector3Int.RoundToInt(worldPosition);
        }

        public static (int chX, int chZ) GetChunkPosition(Vector3 worldPosition) {
            return GetChunkPosition(worldPosition.x, worldPosition.z);
        }

        public static (int chX, int chZ) GetChunkPosition(float x, float z) {
            int cX = Mathf.FloorToInt(x);
            int cZ = Mathf.FloorToInt(z);
            int chX = Mathf.FloorToInt(cX / 16f);
            int chZ = Mathf.FloorToInt(cZ / 16f);
            return (chX, chZ);
        }
    }
}