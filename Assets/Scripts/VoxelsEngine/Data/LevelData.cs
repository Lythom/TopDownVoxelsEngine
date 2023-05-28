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
    public const int LevelChunkSize = 256;
    public readonly ChunkData[,] Chunks;

    public ConcurrentQueue<ChunkKey> CreationQueue = new() {
    };

    public string SaveId;
    public string LevelId;

    private CancellationTokenSource cts = new();

    public LevelData(string saveId, string levelId) {
        LevelId = levelId;
        SaveId = saveId;
        GenerateChunksFromQueue(cts.Token).Forget();

        Chunks = new ChunkData[LevelChunkSize, LevelChunkSize];
        for (int x = 0; x < LevelChunkSize; x++) {
            for (int z = 0; z < LevelChunkSize; z++) {
                Chunks[x, z] = new ChunkData(x, z);
            }
        }
    }


    public void Dispose() {
        cts.Cancel(false);
    }

    private async UniTaskVoid GenerateChunksFromQueue(CancellationToken cancellationToken) {
        Debug.Log("Start job generating chunks");
        while (!cancellationToken.IsCancellationRequested) {
            await UniTask.Yield(PlayerLoopTiming.LastUpdate, cancellationToken);
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

        Debug.Log("Stop job generating chunks");
    }

    public Cell? GetNeighbor(int x, int y, int z, Direction dir) {
        var offset = dir.GetOffset();
        var offsetY = y + offset.y;
        if (offsetY < 0 || offsetY > 6) return null;
        return TryGetExistingCell(x + offset.x, offsetY, z - offset.z);
        // return await GetOrCreateCell(x + offset.x, offsetY, z - offset.z);
    }

    public ChunkData GenerateChunk(ChunkKey key) {
        var seed = GetChunkSeed(key);
        var rng = new Random(seed);
        // Generate a new chunk
        var chunk = Chunks[key.ChX, key.ChZ];
        if (chunk.Cells == null) {
            chunk.Cells = new Cell[16, 7, 16];
            foreach (var (x, y, z) in chunk.GetCellPositions()) {
                chunk.Cells[x, y, z] = new Cell(BlockDefId.Air);
            }
        }
        // TODO: debug why rendering doesn work anymore now we moved to the "middle" of the grid
        //     TODO: prevent bug when reching borders (return and handle null instead of generating)
        //         TODO: teleport player and camera during awake dynamically

        // ... chunk generation code
        for (int x = 0; x < 16; x++) {
            for (int z = 0; z < 16; z++) {
                // Put a wall sometimes
                var cell = chunk.Cells[x, 4, z];
                double chances = 0.08;
                if (x > 0) {
                    var left = chunk.Cells[x - 1, 4, z];
                    if (left.BlockDef != BlockDefId.Air)
                        chances = 0.6;
                }

                cell.BlockDef = rng.NextDouble() < chances ? BlockDefId.Stone : BlockDefId.Air;
                chunk.Cells[x, 4, z] = cell;
                if (cell.BlockDef == BlockDefId.Stone) {
                    var wallTop = chunk.Cells[x, 5, z];
                    wallTop.BlockDef = rng.NextDouble() < 0.9 ? BlockDefId.Stone : BlockDefId.Air;
                    chunk.Cells[x, 5, z] = wallTop;
                    if (wallTop.BlockDef == BlockDefId.Stone) {
                        var wallTipTop = chunk.Cells[x, 6, z];
                        wallTipTop.BlockDef = rng.NextDouble() < 0.75 ? BlockDefId.Snow : BlockDefId.Air;
                        chunk.Cells[x, 6, z] = wallTipTop;
                    }
                }

                // Ground everywhere
                chunk.Cells[x, 3, z].BlockDef = BlockDefId.Grass;
                chunk.Cells[x, 2, z].BlockDef = BlockDefId.Dirt;
                chunk.Cells[x, 1, z].BlockDef = BlockDefId.Dirt;
                chunk.Cells[x, 0, z].BlockDef = BlockDefId.Dirt;
            }
        }

        chunk.IsGenerated = true;
        // Please: save to file
        Chunks[key.ChX, key.ChZ] = chunk;
        return chunk;
    }

    public async UniTask<ChunkData?> GetChunkFromFile(string saveId, string levelId, int chX, int chZ) {
        var key = ChunkData.GetKey(saveId, levelId, chX, chZ);
        var savePath = GetSavePath(saveId, key);
        if (File.Exists(savePath)) {
            Console.WriteLine($"loading from file {chX},{chZ}");
            var chunk = new ChunkData(chX, chZ);
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

    public async UniTask<ChunkData> GetOrGenerateChunk(int chX, int chZ) {
        Debug.Log($"GetOrGenerateChunk({chX}, {chZ})");

        var key = ChunkData.GetKey(SaveId, LevelId, chX, chZ);
        if (CreationQueue.Contains(key)) await UniTask.WaitUntil(() => !CreationQueue.Contains(key));

        var chunk = Chunks[chX, chZ];
        if (chunk.IsGenerated) {
            return chunk;
        }

        CreationQueue.Enqueue(new(SaveId, LevelId, chX, chZ));
        await UniTask.WaitUntil(() => Chunks[chX, chZ].IsGenerated, PlayerLoopTiming.PostLateUpdate);
        return Chunks[chX, chZ];
    }


    public Cell? TryGetExistingCell(int x, int y, int z) {
        var chX = (int) Math.Floor((double) x / 16);
        var chZ = (int) Math.Floor((double) z / 16);
        var chunk = Chunks[chX, chZ];
        if (chunk.IsGenerated) {
            return chunk.Cells?[mod(x, 16), y, mod(z, 16)];
        }

        return null;
    }

    public async UniTask<Cell?> GetOrCreateCell(int x, int y, int z) {
        var chX = (int) Math.Floor((double) x / 16);
        var chZ = (int) Math.Floor((double) z / 16);
        var chunk = await GetOrGenerateChunk(chX, chZ);
        return chunk.Cells?[mod(x, 16), y, mod(z, 16)];
    }

    static int mod(int x, int m) {
        int r = x % m;
        return r < 0 ? r + m : r;
    }
}