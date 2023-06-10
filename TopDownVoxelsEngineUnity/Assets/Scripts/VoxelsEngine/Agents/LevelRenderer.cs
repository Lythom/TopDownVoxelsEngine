using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using LoneStoneStudio.Tools;
using Shared;
using Sirenix.OdinInspector;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace VoxelsEngine {
    public class LevelRenderer : ConnectedBehaviour {
        private LevelMap? _level = null;
        public readonly ChunkRenderer[,] ChunkRenderers = new ChunkRenderer[LevelMap.LevelChunkSize, LevelMap.LevelChunkSize];

        public readonly HashSet<int> RendererChunks = new();
        public readonly Queue<int> ToBeRendererQueue = new();

        public string LevelId = "0";

        [Required]
        public Material BlockMaterial = null!;

        private CancellationToken _cancellationTokenOnDestroy;


        private void Awake() {
            _cancellationTokenOnDestroy = gameObject.GetCancellationTokenOnDestroy();
            RenderChunksFromQueue(_cancellationTokenOnDestroy).Forget();
        }

        private Character? _character = null;

        protected override void OnSetup(GameState state) {
            var playerId = LocalState.Instance.CurrentPlayerId;
            var playerStateSelector = ReactiveHelpers.CreateSelector(
                state.Characters,
                characters => characters.Dictionary.TryGetValue(playerId, out var value) ? value : null,
                null,
                ResetToken
            );
            var playerLevelSelector = new Reactive<string?>(playerStateSelector.Value?.Level.Value);
            playerLevelSelector.BindCompoundValue(playerStateSelector, c => c?.Level, ResetToken);

            Subscribe(playerStateSelector, p => _character = p);
            Subscribe(playerLevelSelector, levelId => {
                _level = null;
                if (levelId == null) return;
                RendererChunks.Clear();
                transform.DestroyChildren();
                _level = state.Levels[levelId];
            });
        }

        public void Update() {
            if (_level == null || _character == null) return;
            var playerPos = _character.Position;
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
            if (_character == null) return;
            var playerPos = _character.Position;
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
                        if (_level == null || chX < 0 || chX >= _level.Chunks.GetLength(0) || chZ < 0 || chZ >= _level.Chunks.GetLength(1)) continue;
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
            if (_level == null || chX < 0 || chX >= _level.Chunks.GetLength(0) || chZ < 0 || chZ >= _level.Chunks.GetLength(1)) return;
            ChunkRenderer cr = ChunkRenderers[chX, chZ];
            if (cr != null) {
                cr.ReCalculateMesh(_level, new ChunkKey(LevelId, chX, chZ));
                cr.UpdateMesh();
            }
        }

        private void RenderChunk(int chX, int chZ) {
            try {
                if (_level == null || chX < 0 || chX >= _level.Chunks.GetLength(0) || chZ < 0 || chZ >= _level.Chunks.GetLength(1)) return;
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
                    chunkRenderer.ReCalculateMesh(_level, new ChunkKey(LevelId, chX, chZ));
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
            chunkGen.Level = _level!;
            return chunkGen;
        }
    }
}