using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VoxelsEngine;
using Random = System.Random;

public class LevelData : IDisposable {
    public ConcurrentDictionary<ChunkKey, ChunkData> Chunks = new();

    public ConcurrentQueue<ChunkKey> CreationQueue = new() {
    };

    public string SaveId;
    public string LevelId;

    private CancellationTokenSource cts = new();

    public LevelData(string saveId, string levelId) {
        LevelId = levelId;
        SaveId = saveId;
        GenerateChunksFromQueue(cts.Token).Forget();
    }


    public void Dispose() {
        cts.Cancel(false);
    }

    private async UniTaskVoid GenerateChunksFromQueue(CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested) {
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            // dequeue until all is generated
            while (CreationQueue.TryDequeue(out ChunkKey key)) {
                try {
                    GenerateChunk(key);
                    await UniTask.NextFrame(cancellationToken);
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }
        }
    }

    public async UniTask<Cell?> GetNeighbor(int x, int y, int z, Direction dir) {
        var offset = dir.GetOffset();
        var offsetY = y + offset.y;
        if (offsetY < 0 || offsetY > 6) return null;
        return TryGetExistingCell(x + offset.x, offsetY, z - offset.z);
        return await GetOrCreateCell(x + offset.x, offsetY, z - offset.z);
    }

    public ChunkData GenerateChunk(ChunkKey key) {
        var seed = GetChunkSeed(key);
        var rng = new Random(seed);
        // Generate a new chunk
        var chunk = new ChunkData(key);
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
        // Please: save to file
        Chunks[key] = chunk;
        return chunk;
    }

    public async UniTask<ChunkData?> GetChunkFromFile(string saveId, string levelId, int chX, int chZ) {
        var key = ChunkData.GetKey(saveId, levelId, chX, chZ);
        var savePath = GetSavePath(saveId, key);
        if (File.Exists(savePath)) {
            Console.WriteLine($"loading from file {chX},{chZ}");
            var chunk = new ChunkData(saveId, levelId, chX, chZ);
            chunk.UnserializeChunk(await File.ReadAllBytesAsync(savePath));
            return chunk;
        } else {
            Console.WriteLine($"load file failed {chX},{chZ}");
            return null;
        }
    }

    private static string GetSavePath(string saveId, ChunkKey key) {
        var filePath = Path.Join(Application.persistentDataPath, "Saves/" + saveId, key.ToString());
        return filePath;
    }

    public int GetChunkSeed(ChunkKey key) {
        return 1337 + key.ChX + 100000 * key.ChZ + key.LevelId.GetHashCode() * 13 + key.SaveId.GetHashCode() * 7;
    }

    // TODO: this is way too slow. Find a way without so many lookups ! MortonCode with lot of preassigned chunks like 128 x 128 ?
    public async UniTask<ChunkData?> GetOrGenerateChunk(int chX, int chZ) {
        var key = ChunkData.GetKey(SaveId, LevelId, chX, chZ);
        if (CreationQueue.Contains(key)) await UniTask.WaitUntil(() => !CreationQueue.Contains(key));

        if (Chunks.ContainsKey(key)) {
            var chunk = Chunks[key];
            return chunk;
        } else {
            ChunkData? chunk = null;
            // ChunkData? chunk = await GetChunkFromFile(SaveId, LevelId, chX, chZ);
            // if (chunk == null) {
            CreationQueue.Enqueue(new(SaveId, LevelId, chX, chZ));
            await UniTask.WaitUntil(() => Chunks.ContainsKey(key));
            return Chunks[key];
            // }

            Chunks[key] = chunk;
            return chunk;
        }
    }


    public Cell? TryGetExistingCell(int x, int y, int z) {
        var chX = (int) Math.Floor((double) x / 16);
        var chZ = (int) Math.Floor((double) z / 16);
        var key = ChunkData.GetKey(SaveId, LevelId, chX, chZ);
        if (Chunks.ContainsKey(key)) {
            var chunk = Chunks[key];
            return chunk.Cells[mod(x, 16), y, mod(z, 16)];
        }

        return null;
    }

    public async UniTask<Cell?> GetOrCreateCell(int x, int y, int z) {
        var chX = (int) Math.Floor((double) x / 16);
        var chZ = (int) Math.Floor((double) z / 16);
        var chunk = await GetOrGenerateChunk(chX, chZ);
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