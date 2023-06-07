using System;
using MessagePack;

namespace Shared {
    [MessagePackObject(true)]
    public class ChunkKey {
        public string LevelId;
        public int ChX;
        public int ChZ;

        public ChunkKey() {
            LevelId = "";
            ChX = 0;
            ChZ = 0;
        }

        public ChunkKey(string levelId, int chX, int chZ) {
            LevelId = levelId;
            ChX = chX;
            ChZ = chZ;
        }

        public override string ToString() => $"{LevelId}_{ChX}_{ChZ}";

        public bool Equals(ChunkKey other)
        {
            return LevelId == other.LevelId && ChX == other.ChX && ChZ == other.ChZ;
        }

        public override bool Equals(object? obj) {
            return obj is ChunkKey other && Equals(other);
        }

        public override int GetHashCode() {
            return HashCode.Combine(LevelId, ChX, ChZ);
        }
    }

    public static class ChunkKeyPool {
        private static readonly SafeObjectPool<ChunkKey> _pool = new();

        public static ChunkKey Get(string levelId, int chX, int chZ) {
            var k = _pool.Get();
            k.LevelId = levelId;
            k.ChX = chX;
            k.ChZ = chZ;
            return k;
        }

        public static void Return(ChunkKey item) {
            _pool.Return(item);
        }
    }
}