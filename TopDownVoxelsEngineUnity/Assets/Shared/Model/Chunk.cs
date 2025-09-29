using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MessagePack;

namespace Shared {
    [MessagePackObject(true)]
    public struct Chunk {
        public const int Size = 16;
        public const int Height = 64;
        public CellV0[,,]? Cells;
        public bool IsGenerated;

        public readonly IEnumerable<CellPosition> GetCellPositions() {
            for (int y = Height - 1; y >= 0; y--) {
                for (int x = 0; x < Size; x++) {
                    for (int z = 0; z < Size; z++) {
                        yield return new(x, y, z);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort GetFlatIndex(int chX, int chZ) {
            return (ushort) (chX + LevelMap.LevelChunkSize * chZ);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetCoordsFromIndex(int flatIndex, out int chX, out int chZ) {
            chX = flatIndex % LevelMap.LevelChunkSize;
            chZ = flatIndex / LevelMap.LevelChunkSize;
        }
    }
}