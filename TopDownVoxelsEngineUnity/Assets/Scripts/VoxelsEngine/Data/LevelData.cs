using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VoxelsEngine;

public class LevelData : IDisposable {
    public const int LevelChunkSize = 256;
    public readonly ChunkData[,] Chunks;

    public ConcurrentQueue<int> GenerationQueue = new() {
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
            var generatedThisFrame = 0;
            await UniTask.Yield(PlayerLoopTiming.LastUpdate, cancellationToken);
            // dequeue until all is generated
            while (GenerationQueue.TryDequeue(out int flatIndex)) {
                try {
                    var (chX, chZ) = ChunkData.GetCoordsFromIndex(flatIndex);
                    LevelBuilder.GenerateTestChunk(chX, chZ, LevelId, SaveId, ref Chunks[chX, chZ]);
                    generatedThisFrame++;
                    if (generatedThisFrame >= 15) {
                        await UniTask.NextFrame(cancellationToken);
                    }
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }
        }

        Debug.Log("Stop job generating chunks");
    }

    public Cell? GetNeighbor(int x, int y, int z, Direction dir) {
        var offset = dir.GetOffset();
        var yWithOffset = y + offset.y;
        if (yWithOffset < 0 || yWithOffset >= ChunkData.Size) return null;
        return TryGetExistingCell(x + offset.x, yWithOffset, z - offset.z, out _, out _, out _);
        // return await GetOrCreateCell(x + offset.x, offsetY, z - offset.z);
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

    public async UniTask<ChunkData> GetOrGenerateChunk(int chX, int chZ) {
        var key = ChunkData.GetFlatIndex(chX, chZ);
        if (GenerationQueue.Contains(key)) await UniTask.WaitUntil(() => !GenerationQueue.Contains(key));

        var chunk = Chunks[chX, chZ];
        if (chunk.IsGenerated) {
            return chunk;
        }

        GenerationQueue.Enqueue(key);
        await UniTask.WaitUntil(() => Chunks[chX, chZ].IsGenerated, PlayerLoopTiming.PostLateUpdate);
        return Chunks[chX, chZ];
    }


    public bool CellMatchDefinition(Vector3Int position, BlockDefId referenceBlock) {
        if (position.y < 0 || position.y >= ChunkData.Size || position.x < 0 || position.x >= LevelChunkSize * ChunkData.Size || position.z < 0 || position.z >= LevelChunkSize * ChunkData.Size) return false;
        var chX = (int) Math.Floor((double) position.x / ChunkData.Size);
        var chZ = (int) Math.Floor((double) position.z / ChunkData.Size);
        var chunk = Chunks[chX, chZ];
        if (chunk.IsGenerated) {
            return chunk.Cells![Mod(position.x, ChunkData.Size), position.y, Mod(position.z, ChunkData.Size)].BlockDef == referenceBlock;
        }

        return false;
    }


    public bool TrySetExistingCell(int x, int y, int z, BlockDefId blockDef) {
        if (y < 0 || y >= ChunkData.Size) return false;
        var (chX, chZ) = LevelTools.GetChunkPosition(x, z);
        var chunk = Chunks[chX, chZ];
        if (chunk.IsGenerated) {
            var cx = Mod(x, ChunkData.Size);
            var cy = y;
            var cz = Mod(z, ChunkData.Size);
            chunk.Cells![cx, cy, cz].BlockDef = blockDef;
            return true;
        }

        return false;
    }

    public Cell? TryGetExistingCell(int x, int y, int z, out int cx, out int cy, out int cz) {
        cx = 0;
        cy = 0;
        cz = 0;

        if (y < 0 || y >= ChunkData.Size) return null;
        var (chX, chZ) = LevelTools.GetChunkPosition(x, z);
        var chunk = Chunks[chX, chZ];
        if (chunk.IsGenerated) {
            cx = Mod(x, ChunkData.Size);
            cy = y;
            cz = Mod(z, ChunkData.Size);
            return chunk.Cells![cx, cy, cz];
        }

        return null;
    }

    public async UniTask<Cell?> GetOrCreateCell(int x, int y, int z) {
        var chX = (int) Math.Floor((double) x / ChunkData.Size);
        var chZ = (int) Math.Floor((double) z / ChunkData.Size);
        var chunk = await GetOrGenerateChunk(chX, chZ);
        return chunk.Cells?[Mod(x, ChunkData.Size), y, Mod(z, ChunkData.Size)];
    }

    /// <summary>
    /// Modulo function that return a correct "offset" when reading into negative values instead of mirroring the rest of the division.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="modulo"></param>
    /// <returns></returns>
    static int Mod(int value, int modulo) {
        int r = value % modulo;
        return r < 0 ? r + modulo : r;
    }
}