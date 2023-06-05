using System;
using System.Collections.Generic;
using System.Threading;

using Shared;
namespace VoxelsEngine {
    public class LevelGenerator {
        public readonly Queue<ChunkKey> ToBeGeneratedQueue = new();

        public void Enqueue(ChunkKey key) {
            ToBeGeneratedQueue.Enqueue(key);
        }

        public void GenerateFromQueue() {
            var playerPos = Player.transform.position;
            var (chX, chZ) = LevelTools.GetChunkPosition(playerPos);

            var range = 2;
            for (int x = -range; x <= range; x++) {
                for (int z = -range; z <= range; z++) {
                    var key = Chunk.GetFlatIndex(chX + x, chZ + z);
                    if (!GeneratedChunks.Contains(key)) {
                        if (chX + x < 0 || chX + x >= _level.Chunks.GetLength(0) || chZ + z < 0 || chZ + z >= _level.Chunks.GetLength(1)) continue;
                        GeneratedChunks.Add(key);
                        ToBeGeneratedQueue.Enqueue(key);
                    }
                }
            }
        }

        private void OnDrawGizmos() {
            var playerPos = Player.transform.position;
            var (chX, chZ) = LevelTools.GetChunkPosition(playerPos);
            Gizmos.DrawWireCube(new Vector3(chX * Chunk.Size + Chunk.Size / 2f, 0, chZ * Chunk.Size + Chunk.Size / 2f),
                new Vector3(Chunk.Size, Chunk.Size, Chunk.Size));
        }

        private async UniTask RenderChunksFromQueue(CancellationToken cancellationToken) {
            Debug.Log("Start job rendering chunks.");

            while (!cancellationToken.IsCancellationRequested) {
                var renderedThisFrame = 0;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                // dequeue until all is generated
                while (ToBeGeneratedQueue.TryDequeue(out int chunkFlatIndex)) {
                    try {
                        var (chX, chZ) = Chunk.GetCoordsFromIndex(chunkFlatIndex);
                        RenderChunk(chX, chZ);
                        renderedThisFrame++;

                        if (renderedThisFrame >= 1) {
                            // max 1 per frame
                            await UniTask.NextFrame(cancellationToken);
                        }
                    } catch (Exception e) {
                        Debug.LogException(e);
                    }
                }
            }

            Debug.Log("Stop job rendering chunks. Cancellation was requested.");
        }

        public void UpdateChunk(int chX, int chZ) {
            if (chX < 0 || chX >= _level.Chunks.GetLength(0) || chZ < 0 || chZ >= _level.Chunks.GetLength(1)) return;
            ChunkGenerated cr = ChunkGenerateds[chX, chZ];
            if (cr != null) {
                cr.ReCalculateMesh(_level);
                cr.UpdateMesh();
            }
        }

        private void RenderChunk(int chX, int chZ) {
            try {
                if (chX < 0 || chX >= _level.Chunks.GetLength(0) || chZ < 0 || chZ >= _level.Chunks.GetLength(1)) return;
                Chunk currentChunk = _level.GetOrGenerateChunk(chX, chZ);
                // preload outbounds chunk content
                for (int x = -1; x <= 1; x++) {
                    for (int z = -1; z <= 1; z++) {
                        if (chX + x < 0 || chX + x >= _level.Chunks.GetLength(0) || chZ + z < 0 || chZ + z >= _level.Chunks.GetLength(1)) continue;
                        _level.GetOrGenerateChunk(chX + x, chZ + z);
                    }
                }

                if (currentChunk.IsGenerated && !_cancellationTokenOnDestroy.IsCancellationRequested) {
                    var chunkGenerated = GenerateChunkGenerated(chX, chZ);
                    chunkGenerated.transform.SetParent(transform, true);
                    chunkGenerated.ReCalculateMesh(_level);
                    chunkGenerated.UpdateMesh();
                    chunkGenerated.transform.localScale = Vector3.zero;
                    chunkGenerated.transform.DOScale(1, 0.3f).SetEase(Ease.OutBack);
                    ChunkGenerateds[chX, chZ] = chunkGenerated;
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        private ChunkGenerated GenerateChunkGenerated(int chX, int chY) {
            var go = new GameObject("Chunk Generated " + chX + "," + chY);
            var f = go.AddComponent<MeshFilter>();
            f.mesh = new Mesh();
            var r = go.AddComponent<MeshGenerated>();
            r.sharedMaterial = BlockMaterial;
            var chunkGen = go.AddComponent<ChunkGenerated>();
            chunkGen.transform.localPosition = new Vector3(chX * Chunk.Size, 0, chY * Chunk.Size);
            chunkGen.Level = _level;
            chunkGen.ChunkKey = new(_level.SaveId, _level.LevelId, chX, chY);
            return chunkGen;
        }

        public Cell? GetCellAt(Vector3 worldPosition) {
            return _level.TryGetExistingCell(
                Mathf.RoundToInt(worldPosition.x),
                Mathf.RoundToInt(worldPosition.y),
                Mathf.RoundToInt(worldPosition.z),
                out _,
                out _,
                out _
            );
        }

        public bool SetCellAt(Vector3 worldPosition, BlockId block) {
            return _level.TrySetExistingCell(
                Mathf.RoundToInt(worldPosition.x),
                Mathf.RoundToInt(worldPosition.y),
                Mathf.RoundToInt(worldPosition.z),
                block
            );
        }
    }
}