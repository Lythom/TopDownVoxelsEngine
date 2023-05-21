using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using VoxelsEngine;

public class LevelData {
    public Dictionary<string, Chunk> Chunks = new();


    public Chunk GenerateChunk(string saveId, string levelId, int chX, int chY, int cX, int cY, int seed) {
        var rng = new System.Random(seed);
        // Generate a new chunk
        var chunk = new Chunk(saveId, levelId, chX, chY);
        // ... chunk generation code
        for (int x = 0; x < 16; x++) {
            for (int y = 0; y < 16; y++) {
                // Put a wall sometimes
                var cell = chunk.Cells[x, y, 4];
                double chances = 0.08;
                if (x > 0) {
                    var left = chunk.Cells[x - 1, y, 4];
                    if (left.BlockDefinition != "AIR")
                        chances = 0.6;
                }

                cell.BlockDefinition = rng.NextDouble() < chances ? "STONE" : "AIR";
                if (cell.BlockDefinition == "STONE") {
                    var wallTop = chunk.Cells[x, y, 5];
                    wallTop.BlockDefinition = rng.NextDouble() < 0.9 ? "STONE" : "AIR";
                    if (wallTop.BlockDefinition == "STONE") {
                        var wallTipTop = chunk.Cells[x, y, 6];
                        wallTipTop.BlockDefinition = rng.NextDouble() < 0.75 ? "SNOW" : "AIR";
                    }
                }

                // Ground everywhere
                var ground = chunk.Cells[x, y, 3];
                ground.BlockDefinition = "DIRT";
            }
        }

        chunk.IsLoaded = true;
        var key = chunk.GetKey();
        // Please: save to file
        Chunks[key] = chunk;
        return chunk;
    }

    public async ValueTask<Chunk> GetChunkFromFile(string saveId, string levelId, int chX, int chY) {
        var key = Chunk.GetKey(saveId, levelId, chX, chY);
        var savePath = GetSavePath(saveId, key);
        if (File.Exists(savePath)) {
            Console.WriteLine($"loading from file {chX},{chY}");
            var chunk = new Chunk(saveId, levelId, chX, chY);
            chunk.UnserializeChunk(await File.ReadAllBytesAsync(savePath));
            return chunk;
        } else {
            Console.WriteLine($"load file failed {chX},{chY}");
            return null;
        }
    }

    private static string GetSavePath(string saveId, string key) {
        var filePath = Path.Join(Application.persistentDataPath, "Saves/" + saveId, key);
        return filePath;
    }

    public async ValueTask<Chunk?> GetOrGenerateChunk(string saveId, string levelId, int chX, int chY, int pX, int pY, int seed) {
        var key = Chunk.GetKey(saveId, levelId, chX, chY);
        if (Chunks.ContainsKey(key)) {
            var chunk = Chunks[key];
            return chunk;
        } else {
            Chunk chunk = await GetChunkFromFile(saveId, levelId, chX, chY);
            if (chunk == null) {
                chunk = GenerateChunk(saveId, levelId, chX, chY, pX, pY, seed);
            }

            Chunks[key] = chunk;
            return chunk;
        }
    }

    public async ValueTask<Cell?> GetCell(string saveId, string levelId, int x, int y, int z) {
        var chX = (int) Math.Floor((double) x / 16);
        var chY = (int) Math.Floor((double) y / 16);
        var chunk = await GetOrGenerateChunk(saveId, levelId, chX, chY, x, y, 1337);
        if (chunk != null) {
            return chunk.Cells[x % 16, y % 16, z];
        }

        return null;
    }
}