using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;

public class ChunkData {
    public string SaveId;
    public string LevelId;
    public int ChX;
    public int ChZ;
    public Cell[,,] Cells;
    public bool IsGenerated;

    public IEnumerable<CellPosition> GetCellPositions() {
        for (int y = 6; y >= 0; y--) {
            for (int x = 0; x < 16; x++) {
                for (int z = 0; z < 16; z++) {
                    yield return new(x, y, z);
                }
            }
        }
    }

    public ChunkData(ChunkKey key) : this(key.SaveId, key.LevelId, key.ChX, key.ChZ) {
    }

    public ChunkData(string saveId, string levelId, int chX, int chZ) {
        SaveId = saveId;
        LevelId = levelId;
        ChX = chX;
        ChZ = chZ;
        Cells = new Cell[16, 7, 16];
        foreach (var (x, y, z) in GetCellPositions()) {
            Cells[x, y, z] = new Cell("AIR");
        }

        IsGenerated = false;
    }

    public ChunkKey GetKey() {
        return new ChunkKey(SaveId, LevelId, ChX, ChZ);
    }

    public static ChunkKey GetKey(string saveId, string levelId, int chX, int chZ) {
        return new ChunkKey(saveId, levelId, chX, chZ);
    }

    // Note: this requires additional work to handle the serialization
    public byte[] SerializeChunk() {
        MemoryStream ms = new MemoryStream();
        BinaryFormatter formatter = new BinaryFormatter();
        formatter.Serialize(ms, this);
        return ms.ToArray();
    }

    // Note: this requires additional work to handle the deserialization
    public void UnserializeChunk(byte[] data) {
        MemoryStream ms = new MemoryStream(data);
        BinaryFormatter formatter = new BinaryFormatter();
        var chunk = (ChunkData) formatter.Deserialize(ms);
        ChX = chunk.ChX;
        ChZ = chunk.ChZ;
        Cells = chunk.Cells;
        IsGenerated = true;
    }
}