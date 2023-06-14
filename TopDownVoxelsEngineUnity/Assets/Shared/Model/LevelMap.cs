using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;
using LoneStoneStudio.Tools;
using MessagePack;

namespace Shared {
    [MessagePackObject(true)]
    public class LevelMap : IDisposable, IUpdatable<LevelMap> {
        public const int LevelChunkSize = 128;
        public readonly Chunk[,] Chunks = new Chunk[LevelChunkSize, LevelChunkSize];
        public readonly ReactiveList<NPC> Npcs = new();
        public readonly string LevelId;

        private readonly CancellationTokenSource _cts = new();

        public LevelMap() {
        }

        public LevelMap(string levelId) {
            LevelId = levelId;
        }


        public void Dispose() {
            _cts.Cancel(false);
        }

        public Cell? GetNeighbor(int x, int y, int z, Direction dir) {
            var offset = dir.GetOffset();
            var yWithOffset = y + offset.y;
            if (yWithOffset < 0 || yWithOffset >= Chunk.Size) return null;
            return TryGetExistingCell(x + offset.x, yWithOffset, z - offset.z, out _, out _, out _);
            // return await GetOrCreateCell(x + offset.X, offsetY, z - offset.Z);
        }

        // public async UniTask<ChunkData?> GetChunkFromFile(string saveId, string levelId, int chX, int chZ) {
        //     var key = ChunkData.GetKey(saveId, levelId, chX, chZ);
        //     var savePath = GetSavePath(saveId, key);
        //     if (File.Exists(savePath)) {
        //         Console.WriteLine($"loading from file {chX},{chZ}");
        //         var chunk = new ChunkData(chX, chZ);
        //         chunk.UnserializeChunk(await File.ReadAllBytesAsync(savePath));
        //         return chunk;
        //     } else {
        //         Console.WriteLine($"load file failed {chX},{chZ}");
        //         return null;
        //     }
        // }

        // private static string GetSavePath(string saveId, ChunkKey key) {
        //     var filePath = Path.Join(Application.persistentDataPath, "Saves/" + saveId, key.ToString());
        //     return filePath;
        // }

        public Chunk GetOrGenerateChunk(int chX, int chZ) {
            // var key = ChunkData.GetFlatIndex(chX, chZ);
            //if (GenerationQueue.Contains(key)) await WaitUntil(() => !GenerationQueue.Contains(key));

            var chunk = Chunks[chX, chZ];
            if (chunk.IsGenerated) {
                return chunk;
            }

            //GenerationQueue.Enqueue(key);
            // await WaitUntil(() => Chunks[chX, chZ].IsGenerated);
            LevelBuilder.GenerateTestChunk(chX, chZ, LevelId, ref Chunks[chX, chZ]);

            return Chunks[chX, chZ];
        }

        public bool CellMatchDefinition(Vector3Int position, BlockId referenceBlock) {
            if (position.Y < 0 || position.Y >= Chunk.Size || position.X < 0 || position.X >= LevelChunkSize * Chunk.Size || position.Z < 0 ||
                position.Z >= LevelChunkSize * Chunk.Size) return false;
            var chX = (int) Math.Floor((double) position.X / Chunk.Size);
            var chZ = (int) Math.Floor((double) position.Z / Chunk.Size);
            var chunk = Chunks[chX, chZ];
            if (chunk.IsGenerated) {
                return chunk.Cells![M.Mod(position.X, Chunk.Size), position.Y, M.Mod(position.Z, Chunk.Size)].Block == referenceBlock;
            }

            return false;
        }


        public bool TrySetExistingCell(int x, int y, int z, BlockId block) {
            if (y < 0 || y >= Chunk.Size) return false;
            var (chX, chZ) = LevelTools.GetChunkPosition(x, z);
            var chunk = Chunks[chX, chZ];
            if (chunk.IsGenerated) {
                var (cx, cy, cz) = LevelTools.WorldToCellInChunk(x, y, z);
                chunk.Cells![cx, cy, cz].Block = block;
                return true;
            }

            return false;
        }

        public Cell? TryGetExistingCell(Vector3Int wp) {
            return TryGetExistingCell(wp.X, wp.Y, wp.Z, out _, out _, out _);
        }

        public Cell? TryGetExistingCell(int x, int y, int z, out uint cx, out uint cy, out uint cz) {
            cx = 0;
            cy = 0;
            cz = 0;

            if (y < 0 || y >= Chunk.Size) return null;
            var (chX, chZ) = LevelTools.GetChunkPosition(x, z);
            if (chX < 0 || chX >= Chunks.GetLength(0) || chZ < 0 || chZ >= Chunks.GetLength(1)) return null;

            var chunk = Chunks[chX, chZ];
            if (chunk.IsGenerated) {
                (cx, cy, cz) = LevelTools.WorldToCellInChunk(x, y, z);
                return chunk.Cells![cx, cy, cz];
            }

            return null;
        }

        public Cell? GetOrCreateCell(int x, int y, int z) {
            var chX = (int) Math.Floor((double) x / Chunk.Size);
            var chZ = (int) Math.Floor((double) z / Chunk.Size);
            if (chX < 0 || chX >= Chunks.GetLength(0) || chZ < 0 || chZ >= Chunks.GetLength(1)) return null;
            var chunk = GetOrGenerateChunk(chX, chZ);
            return chunk.Cells?[M.Mod(x, Chunk.Size), y, M.Mod(z, Chunk.Size)];
        }

        public bool CanSet(Vector3Int p, BlockId selectedItemValue) {
            var c = TryGetExistingCell(p);
            return c != null && c.Value.Block != selectedItemValue;
        }

        public void UpdateValue(LevelMap nextState) {
            Npcs.SynchronizeToTarget(nextState.Npcs);
            var nextStateChunks = nextState.Chunks;
            for (int i = 0; i < Chunks.GetLength(0); i++) {
                for (int j = 0; j < Chunks.GetLength(1); j++) {
                    var nextChunk = nextStateChunks[i, j];
                    if (nextChunk.IsGenerated) Chunks[i, j] = nextChunk;
                }
            }
        }
    }
}