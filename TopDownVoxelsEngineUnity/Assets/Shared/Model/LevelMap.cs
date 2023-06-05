using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MessagePack;

namespace Shared
{
    [MessagePackObject(true)]
    public class LevelMap : IDisposable
    {
        public const int LevelChunkSize = 128;
        public readonly Chunk[,] Chunks;

        public static void Log(string s)
        {
        }

        public static void LogException(Exception e)
        {
        }

        public ConcurrentQueue<int> GenerationQueue = new()
        {
        };

        public string SaveId;
        public string LevelId;

        private CancellationTokenSource cts = new();

        public LevelMap(string saveId, string levelId)
        {
            LevelId = levelId;
            SaveId = saveId;
            GenerateChunksFromQueue(cts.Token).Forget();

            Chunks = new Chunk[LevelChunkSize, LevelChunkSize];
            for (int x = 0; x < LevelChunkSize; x++)
            {
                for (int z = 0; z < LevelChunkSize; z++)
                {
                    Chunks[x, z] = new Chunk()
                    {
                        Cells = new Cell[Chunk.Size, Chunk.Size, Chunk.Size],
                        IsGenerated = false
                    };
                }
            }
        }


        public void Dispose()
        {
            cts.Cancel(false);
        }

        private async UniTaskVoid GenerateChunksFromQueue(CancellationToken cancellationToken)
        {
            Log("Start job generating chunks");
            while (!cancellationToken.IsCancellationRequested)
            {
                await UniTask.Yield();
                // dequeue until all is generated
                while (GenerationQueue.TryDequeue(out int flatIndex))
                {
                    try
                    {
                        var (chX, chZ) = Chunk.GetCoordsFromIndex(flatIndex);
                        LevelBuilder.GenerateTestChunk(chX, chZ, LevelId, SaveId, ref Chunks[chX, chZ]);
                    }
                    catch (Exception e)
                    {
                        LogException(e);
                    }
                }
            }

            Log("Stop job generating chunks");
        }

        public Cell? GetNeighbor(int x, int y, int z, Direction dir)
        {
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

        public Chunk GetOrGenerateChunk(int chX, int chZ)
        {
            // var key = ChunkData.GetFlatIndex(chX, chZ);
            //if (GenerationQueue.Contains(key)) await WaitUntil(() => !GenerationQueue.Contains(key));

            var chunk = Chunks[chX, chZ];
            if (chunk.IsGenerated)
            {
                return chunk;
            }

            //GenerationQueue.Enqueue(key);
            // await WaitUntil(() => Chunks[chX, chZ].IsGenerated);
            LevelBuilder.GenerateTestChunk(chX, chZ, LevelId, SaveId, ref Chunks[chX, chZ]);

            return Chunks[chX, chZ];
        }

        // TODO: super dangerous code, Change design !
        static async UniTask WaitUntil(Func<bool> predicate, int intervalMilliseconds = 10)
        {
            while (!predicate())
            {
                await Task.Delay(intervalMilliseconds);
            }
        }


        public bool CellMatchDefinition(Vector3Int position, BlockId referenceBlock)
        {
            if (position.Y < 0 || position.Y >= Chunk.Size || position.X < 0 || position.X >= LevelChunkSize * Chunk.Size || position.Z < 0 ||
                position.Z >= LevelChunkSize * Chunk.Size) return false;
            var chX = (int) Math.Floor((double) position.X / Chunk.Size);
            var chZ = (int) Math.Floor((double) position.Z / Chunk.Size);
            var chunk = Chunks[chX, chZ];
            if (chunk.IsGenerated)
            {
                return chunk.Cells![Mod(position.X, Chunk.Size), position.Y, Mod(position.Z, Chunk.Size)].Block == referenceBlock;
            }

            return false;
        }


        public bool TrySetExistingCell(int x, int y, int z, BlockId block)
        {
            if (y < 0 || y >= Chunk.Size) return false;
            var (chX, chZ) = LevelTools.GetChunkPosition(x, z);
            var chunk = Chunks[chX, chZ];
            if (chunk.IsGenerated)
            {
                var cx = Mod(x, Chunk.Size);
                var cy = y;
                var cz = Mod(z, Chunk.Size);
                chunk.Cells![cx, cy, cz].Block = block;
                return true;
            }

            return false;
        }

        public Cell? TryGetExistingCell(int x, int y, int z, out int cx, out int cy, out int cz)
        {
            cx = 0;
            cy = 0;
            cz = 0;

            if (y < 0 || y >= Chunk.Size) return null;
            var (chX, chZ) = LevelTools.GetChunkPosition(x, z);
            if (chX < 0 || chX >= Chunks.GetLength(0) || chZ < 0 || chZ >= Chunks.GetLength(1)) return null;

            var chunk = Chunks[chX, chZ];
            if (chunk.IsGenerated)
            {
                cx = Mod(x, Chunk.Size);
                cy = y;
                cz = Mod(z, Chunk.Size);
                return chunk.Cells![cx, cy, cz];
            }

            return null;
        }

        public Cell? GetOrCreateCell(int x, int y, int z)
        {
            var chX = (int) Math.Floor((double) x / Chunk.Size);
            var chZ = (int) Math.Floor((double) z / Chunk.Size);
            if (chX < 0 || chX >= Chunks.GetLength(0) || chZ < 0 || chZ >= Chunks.GetLength(1)) return null;
            var chunk = GetOrGenerateChunk(chX, chZ);
            return chunk.Cells?[Mod(x, Chunk.Size), y, Mod(z, Chunk.Size)];
        }

        /// <summary>
        /// Modulo function that return a correct "offset" when reading into negative values instead of mirroring the rest of the division.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="modulo"></param>
        /// <returns></returns>
        static int Mod(int value, int modulo)
        {
            int r = value % modulo;
            return r < 0 ? r + modulo : r;
        }
    }
}