using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using log4net.Core;
using UnityEngine;
using VoxelsEngine;
using Random = System.Random;

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
                    GenerateChunk(chX, chZ);
                    generatedThisFrame++;
                    if (generatedThisFrame >= 16) {
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
        if (yWithOffset < 0 || yWithOffset > 6) return null;
        return TryGetExistingCell(x + offset.x, yWithOffset, z - offset.z, out _, out _, out _);
        // return await GetOrCreateCell(x + offset.x, offsetY, z - offset.z);
    }

    public ChunkData GenerateChunk(int chX, int chZ) {
        var seed = GetChunkSeed(chX, chZ);
        var rng = new Random(seed);
        // Generate a new chunk
        var chunk = Chunks[chX, chZ];
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
        Chunks[chX, chZ] = chunk;
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

    public int GetChunkSeed(int chX, int chZ) {
        return 1337 + chX + 100000 * chZ + LevelId.GetHashCode() * 13 + SaveId.GetHashCode() * 7;
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
        if (position.y < 0 || position.y >= 7 || position.x < 0 || position.x >= LevelChunkSize * 16 || position.z < 0 || position.z >= LevelChunkSize * 16) return false;
        var chX = (int) Math.Floor((double) position.x / 16);
        var chZ = (int) Math.Floor((double) position.z / 16);
        var chunk = Chunks[chX, chZ];
        if (chunk.IsGenerated) {
            return chunk.Cells![Mod(position.x, 16), position.y, Mod(position.z, 16)].BlockDef == referenceBlock;
        }

        return false;
    }

    public Cell? TryGetExistingCell(int x, int y, int z, out int cx, out int cy, out int cz) {
        var chX = (int) Math.Floor((double) x / 16);
        var chZ = (int) Math.Floor((double) z / 16);
        var chunk = Chunks[chX, chZ];
        if (chunk.IsGenerated) {
            cx = Mod(x, 16);
            cy = y;
            cz = Mod(z, 16);
            return chunk.Cells![cx, cy, cz];
        }

        cx = 0;
        cy = 0;
        cz = 0;
        return null;
    }

    public async UniTask<Cell?> GetOrCreateCell(int x, int y, int z) {
        var chX = (int) Math.Floor((double) x / 16);
        var chZ = (int) Math.Floor((double) z / 16);
        var chunk = await GetOrGenerateChunk(chX, chZ);
        return chunk.Cells?[Mod(x, 16), y, Mod(z, 16)];
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

    public int Get8SurroundingsBitmask(Direction dir, int x, int y, int z, BlockDefId referenceBlock) {
        // take the surrounding blocks positions from "top left" like if we are facing the side.
        // 0 1 2
        // 7 x 3
        // 6 5 4
        // x, y, z are positive when going right, up and forward in unity
        var positions = new List<Vector3Int>(8);
        if (dir == Direction.Up) {
            // For the side facing up, we are looking down
            // z
            // ^
            // |
            // -----> x
            positions.Add(new(x - 1, y, z + 1));
            positions.Add(new(x, y, z + 1));
            positions.Add(new(x + 1, y, z + 1));
            positions.Add(new(x + 1, y, z));
            positions.Add(new(x + 1, y, z - 1));
            positions.Add(new(x, y, z - 1));
            positions.Add(new(x - 1, y, z - 1));
            positions.Add(new(x - 1, y, z));
        } else if (dir == Direction.Down) {
            // For the side facing down, we are looking up
            // -----> x
            // |
            // v
            // z
            positions.Add(new(x - 1, y, z - 1));
            positions.Add(new(x, y, z - 1));
            positions.Add(new(x + 1, y, z - 1));
            positions.Add(new(x + 1, y, z));
            positions.Add(new(x + 1, y, z + 1));
            positions.Add(new(x, y, z + 1));
            positions.Add(new(x - 1, y, z + 1));
            positions.Add(new(x - 1, y, z));
        } else if (dir == Direction.South) {
            // For the side facing North (Forward), we are looking south
            //      y
            //      ^
            //      |
            // x <---
            positions.Add(new(x + 1, y + 1, z));
            positions.Add(new(x, y + 1, z));
            positions.Add(new(x - 1, y + 1, z));
            positions.Add(new(x - 1, y, z));
            positions.Add(new(x - 1, y - 1, z));
            positions.Add(new(x, y - 1, z));
            positions.Add(new(x + 1, y - 1, z));
            positions.Add(new(x + 1, y, z));
        } else if (dir == Direction.North) {
            // For the side facing South (Backward), we are looking north
            // y
            // ^
            // |
            // -----> x
            positions.Add(new(x - 1, y + 1, z));
            positions.Add(new(x, y + 1, z));
            positions.Add(new(x + 1, y + 1, z));
            positions.Add(new(x + 1, y, z));
            positions.Add(new(x + 1, y - 1, z));
            positions.Add(new(x, y - 1, z));
            positions.Add(new(x - 1, y - 1, z));
            positions.Add(new(x - 1, y, z));
        } else if (dir == Direction.West) {
            // For the side facing West (Left), we are looking East
            //      y
            //      ^
            //      |
            // z <---
            positions.Add(new(x, y + 1, z + 1));
            positions.Add(new(x, y + 1, z));
            positions.Add(new(x, y + 1, z - 1));
            positions.Add(new(x, y, z - 1));
            positions.Add(new(x, y - 1, z - 1));
            positions.Add(new(x, y - 1, z));
            positions.Add(new(x, y - 1, z + 1));
            positions.Add(new(x, y, z + 1));
        } else if (dir == Direction.East) {
            // For the side facing East (Right), we are looking West
            // y
            // ^
            // |
            // -----> z
            positions.Add(new(x, y + 1, z - 1));
            positions.Add(new(x, y + 1, z));
            positions.Add(new(x, y + 1, z + 1));
            positions.Add(new(x, y, z + 1));
            positions.Add(new(x, y - 1, z + 1));
            positions.Add(new(x, y - 1, z));
            positions.Add(new(x, y - 1, z - 1));
            positions.Add(new(x, y, z - 1));
        }

        int bitmask = 0;
        for (var i = 0; i < positions.Count; i++) {
            int isSameBlock = CellMatchDefinition(positions[i], referenceBlock) ? 1 : 0;
            bitmask |= isSameBlock << i;
        }

        return bitmask;
    }
}