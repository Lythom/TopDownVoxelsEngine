using System;
using System.Collections.Generic;
using System.Diagnostics;
using Priority_Queue;

namespace Shared {
    public class LevelGenerator {
        public readonly SimplePriorityQueue<ChunkKey, int> ToBeGeneratedQueue = new();
        private readonly Stopwatch _frameStopwatch = new();

        public void GenerateFromQueue(PriorityLevel minPriority, Dictionary<string, LevelMap> levels) {
            _frameStopwatch.Reset();
            int budget = minPriority switch {
                PriorityLevel.Should => 2,
                PriorityLevel.Must => 0,
                _ => 10
            };
            while (_frameStopwatch.ElapsedMilliseconds > budget && ToBeGeneratedQueue.TryDequeue(out var key)) {
                LevelBuilder.GenerateTestChunk(key.ChX, key.ChZ, key.LevelId, ref levels[key.LevelId].Chunks[key.ChX, key.ChZ]);
                ChunkKeyPool.Return(key);
            }
        }

        public void EnqueueChunksAround(string levelId, int chX, int chZ, int range) {
            for (int x = -range; x <= range; x++) {
                for (int z = -range; z <= range; z++) {
                    var key = ChunkKeyPool.Get(levelId, chX + x, chZ + z);
                    if (!ToBeGeneratedQueue.Contains(key)) {
                        if (key.ChX + x < 0 || key.ChX + x >= LevelMap.LevelChunkSize || key.ChZ + z < 0 || key.ChZ + z >= LevelMap.LevelChunkSize) continue;
                        // prioritize by distance to interest point
                        ToBeGeneratedQueue.Enqueue(key, Math.Abs(x) + Math.Abs(z));
                    }
                }
            }
        }
    }
}