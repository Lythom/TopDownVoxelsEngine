using System;
using MessagePack;

namespace Shared {
    [MessagePackObject(true)]
    public readonly struct ChunkKey {
        public readonly string SaveId;
        public readonly string LevelId;
        public readonly int ChX;
        public readonly int ChZ;

        public ChunkKey(string saveId, string levelId, int chX, int chZ) {
            SaveId = saveId;
            LevelId = levelId;
            ChX = chX;
            ChZ = chZ;
        }

        public override string ToString() => $"{SaveId}_{LevelId}_{ChX}_{ChZ}";

        public bool Equals(ChunkKey other) {
            return SaveId == other.SaveId && LevelId == other.LevelId && ChX == other.ChX && ChZ == other.ChZ;
        }

        public override bool Equals(object? obj) {
            return obj is ChunkKey other && Equals(other);
        }

        public override int GetHashCode() {
            return HashCode.Combine(SaveId, LevelId, ChX, ChZ);
        }
    }
}