using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VoxelsEngine;
using Random = System.Random;

public class LevelData {
    public Dictionary<string, Chunk> Chunks = new();
    public string SaveId;
    public string LevelId;

    public LevelData(string saveId, string levelId) {
        LevelId = levelId;
        SaveId = saveId;
    }

    public async UniTask<Cell?> GetNeighbor(int x, int y, int z, Direction dir) {
        var offset = dir.GetOffset();
        var offsetY = y + offset.y;
        if (offsetY < 0 || offsetY > 6) return null;
        return await GetOrCreateCell(x + offset.x, offsetY, z - offset.z);
    }

    public Chunk GenerateChunk(int chX, int chZ, int seed) {
        var rng = new Random(seed);
        // Generate a new chunk
        var chunk = new Chunk(SaveId, LevelId, chX, chZ);
        // ... chunk generation code
        for (int x = 0; x < 16; x++) {
            for (int z = 0; z < 16; z++) {
                // Put a wall sometimes
                var cell = chunk.Cells[x, 4, z];
                double chances = 0.08;
                if (x > 0) {
                    var left = chunk.Cells[x - 1, 4, z];
                    if (left.BlockDefinition != "AIR")
                        chances = 0.6;
                }

                cell.BlockDefinition = rng.NextDouble() < chances ? "STONE" : "AIR";
                chunk.Cells[x, 4, z] = cell;
                if (cell.BlockDefinition == "STONE") {
                    var wallTop = chunk.Cells[x, 5, z];
                    wallTop.BlockDefinition = rng.NextDouble() < 0.9 ? "STONE" : "AIR";
                    chunk.Cells[x, 5, z] = wallTop;
                    if (wallTop.BlockDefinition == "STONE") {
                        var wallTipTop = chunk.Cells[x, 6, z];
                        wallTipTop.BlockDefinition = rng.NextDouble() < 0.75 ? "SNOW" : "AIR";
                        chunk.Cells[x, 6, z] = wallTipTop;
                    }
                }

                // Ground everywhere
                chunk.Cells[x, 3, z].BlockDefinition = "GRASS";
                chunk.Cells[x, 2, z].BlockDefinition = "DIRT";
                chunk.Cells[x, 1, z].BlockDefinition = "DIRT";
                chunk.Cells[x, 0, z].BlockDefinition = "DIRT";
            }
        }

        chunk.IsGenerated = true;
        var key = chunk.GetKey();
        // Please: save to file
        Chunks[key] = chunk;
        return chunk;
    }

    public async UniTask<Chunk?> GetChunkFromFile(string saveId, string levelId, int chX, int chZ) {
        var key = Chunk.GetKey(saveId, levelId, chX, chZ);
        var savePath = GetSavePath(saveId, key);
        if (File.Exists(savePath)) {
            Console.WriteLine($"loading from file {chX},{chZ}");
            var chunk = new Chunk(saveId, levelId, chX, chZ);
            chunk.UnserializeChunk(await File.ReadAllBytesAsync(savePath));
            return chunk;
        } else {
            Console.WriteLine($"load file failed {chX},{chZ}");
            return null;
        }
    }

    private static string GetSavePath(string saveId, string key) {
        var filePath = Path.Join(Application.persistentDataPath, "Saves/" + saveId, key);
        return filePath;
    }

    public async UniTask<Chunk?> GetOrGenerateChunk(int chX, int chZ, int seed) {
        var key = Chunk.GetKey(SaveId, LevelId, chX, chZ);
        if (Chunks.ContainsKey(key)) {
            var chunk = Chunks[key];
            return chunk;
        } else {
            Chunk? chunk = await GetChunkFromFile(SaveId, LevelId, chX, chZ);
            if (chunk == null) {
                chunk = GenerateChunk(chX, chZ, seed);
            }

            Chunks[key] = chunk;
            return chunk;
        }
    }


    public Cell? TryGetExistingCell(int x, int y, int z) {
        if (x < 0) return null;
        if (z < 0) return null;
        if (y < 0) return null;
        var chX = (int) Math.Floor((double) x / 16);
        var chZ = (int) Math.Floor((double) z / 16);
        var key = Chunk.GetKey(SaveId, LevelId, chX, chZ);
        if (Chunks.ContainsKey(key)) {
            var chunk = Chunks[key];
            return chunk.Cells[x % 16, y, z % 16];
        }

        return null;
    }

    public async UniTask<Cell?> GetOrCreateCell(int x, int y, int z) {
        var chX = (int) Math.Floor((double) x / 16);
        var chZ = (int) Math.Floor((double) z / 16);
        var chunk = await GetOrGenerateChunk(chX, chZ, 1337 + chX + 1777777 * chZ);
        if (chunk != null) {
            return chunk.Cells[mod(x, 16), y, mod(z, 16)];
        }

        return null;
    }

    static int mod(int x, int m) {
        int r = x % m;
        return r < 0 ? r + m : r;
    }
}