using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Shared
{
    public struct ChunkData
    {
        public const int Size = 16;
        public Cell[,,]? Cells;
        public bool IsGenerated;

        public IEnumerable<CellPosition> GetCellPositions()
        {
            for (int y = Size - 1; y >= 0; y--)
            {
                for (int x = 0; x < Size; x++)
                {
                    for (int z = 0; z < Size; z++)
                    {
                        yield return new(x, y, z);
                    }
                }
            }
        }

        public static int GetFlatIndex(int chX, int chZ)
        {
            return chX + LevelData.LevelChunkSize * chZ;
        }

        public static (int chX, int chZ) GetCoordsFromIndex(int flatIndex)
        {
            var chX = flatIndex % LevelData.LevelChunkSize;
            var chZ = flatIndex / LevelData.LevelChunkSize;
            return (chX, chZ);
        }

        // Note: this requires additional work to handle the serialization
        public byte[] SerializeChunk()
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(ms, this);
            return ms.ToArray();
        }

        // Note: this requires additional work to handle the deserialization
        public void UnserializeChunk(byte[] data)
        {
            MemoryStream ms = new MemoryStream(data);
            BinaryFormatter formatter = new BinaryFormatter();
            var chunk = (ChunkData) formatter.Deserialize(ms);
            Cells = chunk.Cells;
            IsGenerated = true;
        }
    }
}