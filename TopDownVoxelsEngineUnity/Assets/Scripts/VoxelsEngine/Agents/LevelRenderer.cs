using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Shared;
using Sirenix.OdinInspector;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace VoxelsEngine {
    public class LevelRenderer : MonoBehaviour {
        private LevelMap _level = null!;
        public readonly ChunkRenderer[,] ChunkRenderers = new ChunkRenderer[LevelMap.LevelChunkSize, LevelMap.LevelChunkSize];

        public readonly HashSet<int> RendererChunks = new();
        public readonly Queue<int> ToBeRendererQueue = new();

        public string LevelId = "0";

        [Required]
        public Material BlockMaterial = null!;

        [Required, SceneObjectsOnly]
        public CharacterAgent Player = null!;

        private CancellationToken _cancellationTokenOnDestroy;


        private void Awake() {
            _level = new LevelMap(LevelId);
            _cancellationTokenOnDestroy = gameObject.GetCancellationTokenOnDestroy();
            RenderChunksFromQueue(_cancellationTokenOnDestroy).Forget();
        }

        private void OnDestroy() {
            _level.Dispose();
        }

        public void Update() {
            var playerPos = Player.transform.position;
            var (chX, chZ) = LevelTools.GetChunkPosition(playerPos);

            var range = 3;
            for (int x = -range; x <= range; x++) {
                for (int z = -range; z <= range; z++) {
                    var key = Chunk.GetFlatIndex(chX + x, chZ + z);
                    if (!RendererChunks.Contains(key)) {
                        if (chX + x < 0 || chX + x >= _level.Chunks.GetLength(0) || chZ + z < 0 || chZ + z >= _level.Chunks.GetLength(1)) continue;
                        RendererChunks.Add(key);
                        ToBeRendererQueue.Enqueue(key);
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
                while (ToBeRendererQueue.TryDequeue(out int chunkFlatIndex)) {
                    try {
                        var (chX, chZ) = Chunk.GetCoordsFromIndex(chunkFlatIndex);
                        // if enqueued chunk is out bounds, ignore
                        if (chX < 0 || chX >= _level.Chunks.GetLength(0) || chZ < 0 || chZ >= _level.Chunks.GetLength(1)) continue;
                        var chunk = _level.Chunks[chX, chZ];
                        if (!chunk.IsGenerated) {
                            // Not ready to render, put again in the queue and break until next update
                            ToBeRendererQueue.Enqueue(chunkFlatIndex);
                            break;
                        }

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
            ChunkRenderer cr = ChunkRenderers[chX, chZ];
            if (cr != null) {
                cr.ReCalculateMesh(_level);
                cr.UpdateMesh();
            }
        }

        private void RenderChunk(int chX, int chZ) {
            try {
                if (chX < 0 || chX >= _level.Chunks.GetLength(0) || chZ < 0 || chZ >= _level.Chunks.GetLength(1)) return;
                Chunk currentChunk = _level.Chunks[chX, chZ];
                // preload outbounds chunk content
                for (int x = -1; x <= 1; x++) {
                    for (int z = -1; z <= 1; z++) {
                        if (chX + x < 0 || chX + x >= _level.Chunks.GetLength(0) || chZ + z < 0 || chZ + z >= _level.Chunks.GetLength(1)) continue;
                        _level.GetOrGenerateChunk(chX + x, chZ + z);
                    }
                }

                if (currentChunk.IsGenerated && !_cancellationTokenOnDestroy.IsCancellationRequested) {
                    var chunkRenderer = GenerateChunkRenderer(chX, chZ);
                    chunkRenderer.transform.SetParent(transform, true);
                    chunkRenderer.ReCalculateMesh(_level);
                    chunkRenderer.UpdateMesh();
                    chunkRenderer.transform.localScale = Vector3.zero;
                    chunkRenderer.transform.DOScale(1, 0.3f).SetEase(Ease.OutBack);
                    ChunkRenderers[chX, chZ] = chunkRenderer;
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        private ChunkRenderer GenerateChunkRenderer(int chX, int chY) {
            var go = new GameObject("Chunk Renderer " + chX + "," + chY);
            var f = go.AddComponent<MeshFilter>();
            f.mesh = new Mesh();
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = BlockMaterial;
            var chunkGen = go.AddComponent<ChunkRenderer>();
            chunkGen.transform.localPosition = new Vector3(chX * Chunk.Size, 0, chY * Chunk.Size);
            chunkGen.Level = _level;
            return chunkGen;
        }

        /*
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
        }*/
    }
}