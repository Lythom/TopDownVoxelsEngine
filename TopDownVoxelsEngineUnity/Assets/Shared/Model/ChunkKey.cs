using System;
using MessagePack;

namespace Shared
{
    /// <summary>
    /// An immutable, allocation-free identifier for a chunk.  
    /// </summary>
    [MessagePackObject(true)]
    public readonly struct ChunkKey : IEquatable<ChunkKey>
    {
        public readonly string LevelId;
        public readonly int ChX;
        public readonly int ChZ;

        public ChunkKey(string levelId, int chX, int chZ)
        {
            LevelId = levelId;
            ChX    = chX;
            ChZ    = chZ;
        }

        public override string ToString() => $"{LevelId}_{ChX}_{ChZ}";

        public bool Equals(ChunkKey other) =>
            LevelId == other.LevelId && ChX == other.ChX && ChZ == other.ChZ;

        public override bool Equals(object? obj) =>
            obj is ChunkKey other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(LevelId, ChX, ChZ);
    }
}