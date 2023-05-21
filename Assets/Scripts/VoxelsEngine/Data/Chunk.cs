using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class Chunk {
    public string SaveId;
    public string LevelId;
    public int ChX;
    public int ChY;
    public Cell[,,] Cells;
    public bool IsLoaded;

    public Chunk(string saveId, string levelId, int chX, int chY) {
        SaveId = saveId;
        LevelId = levelId;
        ChX = chX;
        ChY = chY;
        Cells = new Cell[16, 16, 7];
        for (int x = 0; x < 16; x++) {
            for (int y = 0; y < 16; y++) {
                for (int z = 0; z < 7; z++) {
                    Cells[x, y, z] = new Cell("AIR");
                }
            }
        }

        IsLoaded = false;
    }

    public string GetKey() {
        return GetKey(SaveId, LevelId, ChX, ChY);
    }

    public static string GetKey(string saveId, string levelId, int chX, int chY) {
        return $"{saveId}_{levelId}_{chX}_{chY}";
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
        ChY = chunk.ChY;
        Cells = chunk.Cells;
        IsLoaded = true;
    }
}