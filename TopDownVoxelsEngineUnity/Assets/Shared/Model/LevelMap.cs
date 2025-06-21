using System;
using System.Threading;
using MessagePack;
using Shared.Signals;

namespace Shared {
    [MessagePackObject(true)]
    public class LevelMap : IDisposable, IUpdatable<LevelMap>, ILevelMap {
        public const int LevelChunkSize = 128;

        public readonly string LevelId = "";
        public Vector3 SpawnPosition;
        public readonly SignalList<NPC> Npcs = new();
        private readonly Chunk[,] _chunks = new Chunk[LevelChunkSize, LevelChunkSize];
        public Chunk[,] Chunks => _chunks;

        private readonly CancellationTokenSource _cts = new();

        public LevelMap() {
        }

        public LevelMap(string levelId, Vector3 spawnPosition) {
            LevelId = levelId;
            SpawnPosition = spawnPosition;
        }

        [SerializationConstructor]
        public LevelMap(string levelId, Vector3 spawnPosition, SignalList<NPC> npcs, Chunk[,] chunks) {
            LevelId = levelId;
            SpawnPosition = spawnPosition;
            _chunks = chunks;
            Npcs = npcs;
        }

        public void Dispose() {
            _cts.Cancel(false);
        }

        public CellV0? GetNeighbor(int x, int y, int z, Direction dir) {
            var offset = dir.GetOffset();
            var yWithOffset = y + offset.y;
            if (yWithOffset < 0 || yWithOffset >= Chunk.Height) return null;
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

        public bool CellMatchDefinition(Vector3Int position, BlockId referenceBlock) {
            if (position.Y < 0 || position.Y >= Chunk.Height || position.X < 0 || position.X >= LevelChunkSize * Chunk.Size || position.Z < 0 ||
                position.Z >= LevelChunkSize * Chunk.Size) return false;
            var chX = (int) Math.Floor((double) position.X / Chunk.Size);
            var chZ = (int) Math.Floor((double) position.Z / Chunk.Size);
            var chunk = _chunks[chX, chZ];
            if (chunk.IsGenerated) {
                var cell = chunk.Cells![M.Mod(position.X, Chunk.Size), position.Y, M.Mod(position.Z, Chunk.Size)];
                if (cell.IsAir()) return referenceBlock.IsAir();
                return cell.Block == referenceBlock;
            }

            return false;
        }


        public bool TrySetExistingCell(int x, int y, int z, BlockId block) {
            if (y < 0 || y >= Chunk.Height) return false;
            var (chX, chZ) = LevelTools.GetChunkPosition(x, z);
            var chunk = _chunks[chX, chZ];
            if (chunk.IsGenerated) {
                LevelTools.WorldToCellInChunk(x, y, z, out var cx, out var cy, out var cz);
                chunk.Cells![cx, cy, cz].Block = block;
                return true;
            }

            return false;
        }

        public void Clear() {
            foreach (var chunk in _chunks) {
                if (chunk.Cells is null) continue;
                Array.Clear(chunk.Cells, 0, chunk.Cells.Length);
            }
        }

        public CellV0? TryGetExistingCell(Vector3Int wp) {
            return TryGetExistingCell(wp.X, wp.Y, wp.Z, out _, out _, out _);
        }

        public CellV0? TryGetExistingCell(int x, int y, int z, out uint cx, out uint cy, out uint cz) {
            cx = 0;
            cy = 0;
            cz = 0;

            if (y < 0 || y >= Chunk.Height) return null;
            var (chX, chZ) = LevelTools.GetChunkPosition(x, z);
            if (chX < 0 || chX >= _chunks.GetLength(0) || chZ < 0 || chZ >= _chunks.GetLength(1)) return null;

            var chunk = _chunks[chX, chZ];
            if (chunk.IsGenerated) {
                LevelTools.WorldToCellInChunk(x, y, z, out cx, out cy, out cz);
                return chunk.Cells![cx, cy, cz];
            }

            return null;
        }

        public bool CanSet(Vector3Int p, BlockId selectedItemValue) {
            var c = TryGetExistingCell(p);
            return c != null && c.Value.Block != selectedItemValue;
        }

        public void UpdateValue(LevelMap nextState) {
            Npcs.SynchronizeToTarget(nextState.Npcs);
            var nextStateChunks = nextState._chunks;
            for (int i = 0; i < _chunks.GetLength(0); i++) {
                for (int j = 0; j < _chunks.GetLength(1); j++) {
                    var nextChunk = nextStateChunks[i, j];
                    if (nextChunk.IsGenerated) _chunks[i, j] = nextChunk;
                }
            }
        }
    }

}