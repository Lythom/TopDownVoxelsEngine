using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Shared;
using Shared.SideEffects;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using Vector3 = UnityEngine.Vector3;

namespace VoxelsEngine {
    public class LevelRenderer : ConnectedBehaviour {
        private LevelMap? _level = null;
        public readonly ChunkRenderer[,] ChunkRenderers = new ChunkRenderer[LevelMap.LevelChunkSize, LevelMap.LevelChunkSize];

        private readonly HashSet<int> _renderedChunks = new();
        private readonly Queue<int> _toBeRendererQueue = new();
        private readonly HashSet<int> _dirtySet = new();

        private Character? _character = null;
        private ChunkGPUSynchronizer? _gpuSynchronizer;

        // Object pool for ChunkRenderer instances
        private ObjectPool<ChunkRenderer>? _chunkRendererPool;

        public string LevelId = "0";

        [Required]
        public Camera Cam = null!;

        [Required]
        public Material BlockMaterial = null!;

        [SerializeField]
        private int _poolWarmupCount = 1024;

        [SerializeField]
        private int _poolMaxSize = 2048;

        private CancellationToken _cancellationTokenOnDestroy;

        private void Awake() {
            _cancellationTokenOnDestroy = gameObject.GetCancellationTokenOnDestroy();
            _gpuSynchronizer = new ChunkGPUSynchronizer();

            RenderChunksFromQueue(_cancellationTokenOnDestroy).Forget();
        }

        protected override void OnSetup(GameState state) {
            Subscribe(state.Selectors.LocalPlayerStateSelector, p => _character = p);
            Subscribe(state.Selectors.LocalPlayerLevelIdSelector, levelId => {
                if (levelId == null) return;

                // Clean up existing renderers
                CleanupRenderers();

                _level = state.Levels[levelId];

                // Initialize the object pool
                InitializeObjectPool();
            });

            SubscribeSideEffect<ChunkDirtySEffect>(cse => SetDirty(cse.ChX, cse.ChZ));
        }

        private void InitializeObjectPool() {
            var time = Time.unscaledTime;
            // Dispose previous pool if exists
            if (_chunkRendererPool != null) {
                _chunkRendererPool.Dispose();
                _chunkRendererPool = null;
            }

            if (_level == null) return;

            // Create a new pool with Unity's built-in ObjectPool
            _chunkRendererPool = new ObjectPool<ChunkRenderer>(
                createFunc: CreateChunkRenderer,
                actionOnGet: OnChunkRendererGet,
                actionOnRelease: OnChunkRendererRelease,
                actionOnDestroy: OnChunkRendererDestroy,
                collectionCheck: true,
                defaultCapacity: _poolWarmupCount,
                maxSize: _poolMaxSize
            );

            // Warm up the pool
            var warmupList = new List<ChunkRenderer>(_poolWarmupCount);
            for (int i = 0; i < _poolWarmupCount; i++) {
                warmupList.Add(_chunkRendererPool.Get());
            }

            // Return all warmed up renderers to the pool
            foreach (var renderer in warmupList) {
                _chunkRendererPool.Release(renderer);
            }

            var time2 = Time.unscaledTime;
            Debug.Log($"ChunkRenderer pool warmed up with {_poolWarmupCount} instances in {time2 - time} seconds.");
        }

        private ChunkRenderer CreateChunkRenderer() {
            var go = new GameObject("Pooled Chunk Renderer");
            go.transform.SetParent(transform);

            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.mesh = new Mesh();

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = BlockMaterial;

            var chunkRenderer = go.AddComponent<ChunkRenderer>();
            chunkRenderer.Level = _level!;
            chunkRenderer.ChunkGPUSynchronizer = _gpuSynchronizer;

            return chunkRenderer;
        }

        private void OnChunkRendererGet(ChunkRenderer chunkRenderer) {
            chunkRenderer.gameObject.SetActive(true);
        }

        private void OnChunkRendererRelease(ChunkRenderer chunkRenderer) {
            chunkRenderer.gameObject.SetActive(false);
            chunkRenderer.transform.SetParent(transform);
        }

        private void OnChunkRendererDestroy(ChunkRenderer chunkRenderer) {
            if (chunkRenderer != null) {
                Destroy(chunkRenderer.gameObject);
            }
        }

        private void CleanupRenderers() {
            // Return all active renderers to the pool if possible
            if (_chunkRendererPool != null) {
                for (int x = 0; x < ChunkRenderers.GetLength(0); x++) {
                    for (int z = 0; z < ChunkRenderers.GetLength(1); z++) {
                        if (ChunkRenderers[x, z] != null) {
                            _chunkRendererPool.Release(ChunkRenderers[x, z]);
                            ChunkRenderers[x, z] = null;
                        }
                    }
                }
            }

            _renderedChunks.Clear();
            _toBeRendererQueue.Clear();
            _dirtySet.Clear();
            _level = null;
        }

        [Button]
        public void ForceRerender() {
            foreach (var r in _renderedChunks) _dirtySet.Add(r);
        }

        public void SetDirty(int chX, int chZ) {
            if (_level == null || chX < 0 || chX >= _level.Chunks.GetLength(0) || chZ < 0 || chZ >= _level.Chunks.GetLength(1)) return;
            _dirtySet.Add(Chunk.GetFlatIndex(chX, chZ));
        }

        public void Update() {
            if (_level == null || _character == null) return;
            UpdateAroundPlayer(_character.Position, _level.Chunks);
            // DrawVegetation();
        }

        private void UpdateAroundPlayer(Shared.Vector3 characterPosition, Chunk[,] levelChunks) {
            var playerPos = characterPosition;
            var (chX, chZ) = LevelTools.GetChunkPosition(playerPos);

            var range = Configurator.Instance.RenderDistance;
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
            Handles.Label(center, $"({chX}, {chZ})");
        }
#endif

        private async UniTask RenderChunksFromQueue(CancellationToken cancellationToken) {
            Debug.Log("Start job rendering chunks.");
            int maxRenderPerFrame = 3;

            while (!cancellationToken.IsCancellationRequested) {
                var renderedThisFrame = 0;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                if (_level == null || _character == null || _chunkRendererPool == null) continue; // skip while level and character are not ready

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
                cr.UpdateMesh(_level, new ChunkKey(LevelId, chX, chZ), ClientEngine.State.BlockPathById);
            }
        }

        private void RenderChunk(int chX, int chZ) {
            try {
                if (_level == null || _chunkRendererPool == null ||
                    chX < 0 || chX >= _level.Chunks.GetLength(0) ||
                    chZ < 0 || chZ >= _level.Chunks.GetLength(1)) return;

                Chunk currentChunk = _level.Chunks[chX, chZ];

                if (currentChunk.IsGenerated && !_cancellationTokenOnDestroy.IsCancellationRequested) {
                    // Get a chunk renderer from the pool
                    var chunkRenderer = _chunkRendererPool.Get();
                    chunkRenderer.transform.SetParent(transform, true);
                    chunkRenderer.transform.localPosition = new Vector3(chX * Chunk.Size, 0, chZ * Chunk.Size);
                    chunkRenderer.gameObject.name = $"Chunk Renderer {chX},{chZ}";

                    chunkRenderer.UpdateMesh(_level, new ChunkKey(LevelId, chX, chZ), ClientEngine.State.BlockPathById);
                    chunkRenderer.transform.localScale = Vector3.zero;
                    chunkRenderer.transform.DOScale(1, 0.3f).SetEase(Ease.OutBack);
                    ChunkRenderers[chX, chZ] = chunkRenderer;
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        private void OnDestroy() {
            CleanupRenderers();
            _chunkRendererPool?.Dispose();
        }
    }
}