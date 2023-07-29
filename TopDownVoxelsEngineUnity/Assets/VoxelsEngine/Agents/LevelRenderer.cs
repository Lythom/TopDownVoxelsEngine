using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using LoneStoneStudio.Tools;
using Shared;
using Shared.SideEffects;
using Sirenix.OdinInspector;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace VoxelsEngine {
    public class LevelRenderer : ConnectedBehaviour {
        public int MaxRenderDistance = 4;

        private LevelMap? _level = null;
        public readonly ChunkRenderer[,] ChunkRenderers = new ChunkRenderer[LevelMap.LevelChunkSize, LevelMap.LevelChunkSize];

        private readonly HashSet<int> _renderedChunks = new();
        private readonly Queue<int> _toBeRendererQueue = new();
        private readonly HashSet<int> _dirtySet = new();

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
            Subscribe(state.Selectors.LocalPlayerStateSelector, p => _character = p);
            Subscribe(state.Selectors.LocalPlayerLevelIdSelector, levelId => {
                _level = null;
                if (levelId == null) return;
                _renderedChunks.Clear();
                transform.DestroyChildren();
                _level = state.Levels[levelId];
            });

            SubscribeSideEffect<ChunkDirtySEffect>(cse => SetDirty(cse.ChX, cse.ChZ));
        }

        public void SetDirty(int chX, int chZ) {
            if (_level == null || chX < 0 || chX >= _level.Chunks.GetLength(0) || chZ < 0 || chZ >= _level.Chunks.GetLength(1)) return;
            _dirtySet.Add(Chunk.GetFlatIndex(chX, chZ));
        }

        public void Update() {
            if (_level == null || _character == null) return;
            UpdateAroundPlayer(_character.Position, _level.Chunks);
        }

        private void UpdateAroundPlayer(Shared.Vector3 characterPosition, Chunk[,] levelChunks) {
            var playerPos = characterPosition;
            var (chX, chZ) = LevelTools.GetChunkPosition(playerPos);

            var range = 3;
            for (int x = -range; x <= range; x++) {
                for (int z = -range; z <= range; z++) {
                    var key = Chunk.GetFlatIndex(chX + x, chZ + z);
                    if (!_renderedChunks.Contains(key)) {
                        if (chX + x < 0 || chX + x >= levelChunks.GetLength(0) || chZ + z < 0 || chZ + z >= levelChunks.GetLength(1)) continue;
                        _renderedChunks.Add(key);
                        _toBeRendererQueue.Enqueue(key);
                    }
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            if (_character == null) return;
            var playerPos = _character.Position;
            var (chX, chZ) = LevelTools.GetChunkPosition(playerPos);
            var center = new Vector3(chX * Chunk.Size + Chunk.Size / 2f - 0.5f, 0, chZ * Chunk.Size + Chunk.Size / 2f - 0.5f);
            Gizmos.DrawWireCube(center, new Vector3(Chunk.Size, Chunk.Size, Chunk.Size));
            UnityEditor.Handles.Label(center, $"({chX}, {chZ})");
        }
#endif

        private async UniTask RenderChunksFromQueue(CancellationToken cancellationToken) {
            Debug.Log("Start job rendering chunks.");
            int maxRenderPerFrame = 3;

            while (!cancellationToken.IsCancellationRequested) {
                var renderedThisFrame = 0;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                if (_level == null || _character == null) continue; // skip while level and character are not ready

                // first update visible chunks that requires rerender
                foreach (var i in _dirtySet) {
                    var (chX, chZ) = Chunk.GetCoordsFromIndex(i);
                    UpdateChunk(chX, chZ);
                }

                _dirtySet.Clear();


                // dequeue until all is generated
                while (_toBeRendererQueue.TryDequeue(out int chunkFlatIndex)) {
                    try {
                        var (chX, chZ) = Chunk.GetCoordsFromIndex(chunkFlatIndex);
                        var chunk = _level.Chunks[chX, chZ];
                        if (!chunk.IsGenerated) {
                            // Not ready to render, put again in the queue and break until next update
                            _toBeRendererQueue.Enqueue(chunkFlatIndex);
                            break;
                        }

                        RenderChunk(chX, chZ);
                        renderedThisFrame++;

                        if (renderedThisFrame >= maxRenderPerFrame) {
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
                // for (int x = -1; x <= 1; x++) {
                //     for (int z = -1; z <= 1; z++) {
                //         if (chX + x < 0 || chX + x >= _level.Chunks.GetLength(0) || chZ + z < 0 || chZ + z >= _level.Chunks.GetLength(1)) continue;
                //         _level.GetOrGenerateChunk(chX + x, chZ + z);
                //     }
                // }

                if (currentChunk.IsGenerated && !_cancellationTokenOnDestroy.IsCancellationRequested) {
                    // TODO: mettre une mécanique pour empêcher une concurrence d'accès
                    var chunkRenderer = InstantiateChunkRenderer(chX, chZ);
                    foreach (Transform child in transform) {
                        if (child.name == chunkRenderer.gameObject.name) {
                            Debug.Log("whaaat ?");
                        }
                    }

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

        private ChunkRenderer InstantiateChunkRenderer(int chX, int chY) {
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