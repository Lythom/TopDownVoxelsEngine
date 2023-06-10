using System.Collections.Generic;
using MessagePack;

namespace Shared {
    [MessagePackObject(true)]
    public struct Chunk {
        public const int Size = 16;
        public Cell[,,] Cells;
        public bool IsGenerated;

        public IEnumerable<CellPosition> GetCellPositions() {
            for (int y = Size - 1; y >= 0; y--) {
                for (int x = 0; x < Size; x++) {
                    for (int z = 0; z < Size; z++) {
                        yield return new(x, y, z);
                    }
                }
            }
        }

        public static int GetFlatIndex(int chX, int chZ) {
            return chX + LevelMap.LevelChunkSize * chZ;
        }

        public static (int chX, int chZ) GetCoordsFromIndex(int flatIndex) {
            var chX = flatIndex % LevelMap.LevelChunkSize;
            var chZ = flatIndex / LevelMap.LevelChunkSize;
            return (chX, chZ);
        }
    }
}