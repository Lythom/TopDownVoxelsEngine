using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class Chunk {
    public string SaveId;
    public string LevelId;
    public int ChX;
    public int ChZ;
    public Cell[,,] Cells;
    public bool IsLoaded;

    public IEnumerable<CellPosition> GetCellPositions() {
        for (int y = 6; y >= 0; y--) {
            for (int x = 0; x < 16; x++) {
                for (int z = 0; z < 16; z++) {
                    yield return new(x, y, z);
                }
            }
        }
    }

    public Chunk(string saveId, string levelId, int chX, int chZ) {
        SaveId = saveId;
        LevelId = levelId;
        ChX = chX;
        ChZ = chZ;
        Cells = new Cell[16, 7, 16];
        foreach (var (x, y, z) in GetCellPositions()) {
            Cells[x, y, z] = new Cell("AIR");
        }

        IsLoaded = false;
    }

    public string GetKey() {
        return GetKey(SaveId, LevelId, ChX, ChZ);
    }

    public static string GetKey(string saveId, string levelId, int chX, int chZ) {
        return $"{saveId}_{levelId}_{chX}_{chZ}";
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
        var chunk = (Chunk) formatter.Deserialize(ms);
        ChX = chunk.ChX;
        ChZ = chunk.ChZ;
        Cells = chunk.Cells;
        IsLoaded = true;
    }
}