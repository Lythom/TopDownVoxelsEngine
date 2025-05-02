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
using UnityEngine.Rendering;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

namespace VoxelsEngine {
    public class LevelRenderer : ConnectedBehaviour {
        private LevelMap? _level = null;
        public readonly ChunkRenderer[,] ChunkRenderers = new ChunkRenderer[LevelMap.LevelChunkSize, LevelMap.LevelChunkSize];

        public bool DEBUG_DisabledFrustrumCulling = false;

        private readonly HashSet<int> _renderedChunks = new();
        private readonly Queue<int> _toBeRendererQueue = new();
        private readonly HashSet<int> _dirtySet = new();

        private List<Matrix4x4> _grass = new();
        public const int GrassMaxInstances = 1000;
        private Matrix4x4[] _grassToDisplay = new Matrix4x4[GrassMaxInstances];
        private Plane[] _cameraFrustumPlanes = new Plane[6];
        public float GrassProximityThreshold = 100;
        public float FrustrumTolerance = 0.5f;

        public string LevelId = "0";

        [Required]
        public Camera Cam = null!;

        [Required]
        public Material BlockMaterial = null!;

        private CancellationToken _cancellationTokenOnDestroy;


        private void Awake() {
            _cancellationTokenOnDestroy = gameObject.GetCancellationTokenOnDestroy();
            RenderChunksFromQueue(_cancellationTokenOnDestroy).Forget();
        }

        private Character? _character = null;
        private Mesh _mesh;

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

        private void DrawVegetation() {
            // occlusion culling
            var cameraPosition = Cam.transform.position;
            CalculateFrustrumPlane(_cameraFrustumPlanes);

            var i = 0;
            foreach (var m in _grass) {
                if (i >= GrassMaxInstances) break; // Limit to GrassMaxInstances instances
                var position = m.GetColumn(3);
                if (DEBUG_DisabledFrustrumCulling || IsInFrustum(position, _cameraFrustumPlanes)) {
                    Random.InitState((int) (position.x + position.y + position.z));
                    if (WithProbabilisticCull(position, cameraPosition, _grass.Count)) {
                        _grassToDisplay[i] = m;
                        i++;
                    }
                }
            }

            // TODO: distance scattering (reduce count ie. 1/2 on distance)
            // TODO: tint variation (perlin)
            // TODO: wind
            if (_mesh != null && i > 0) Graphics.DrawMeshInstanced(_mesh, 0, Configurator.Instance.GrassMat, _grassToDisplay, i - 1, null, ShadowCastingMode.Off, true);
        }

        private void CalculateFrustrumPlane(Plane[] cameraFrustumPlanes) {
            GeometryUtility.CalculateFrustumPlanes(Cam, cameraFrustumPlanes);
            for (int iPlane = 0; iPlane < cameraFrustumPlanes.Length; iPlane++) {
                Plane plane = cameraFrustumPlanes[iPlane];
                Vector3 normal = plane.normal;
                float distance = plane.distance;
                distance += FrustrumTolerance; // Move the plane outward by the tolerance amount
                cameraFrustumPlanes[iPlane] = new Plane(normal, distance);
            }
        }

        private bool WithProbabilisticCull(Vector3 instancePosition, Vector3 cameraPosition, int totalCount) {
            if (totalCount <= GrassMaxInstances) return true; // always display if less that max elements are to be displayed
            var distance = (instancePosition - cameraPosition).sqrMagnitude;
            var percentToShow = (float) GrassMaxInstances / totalCount;
            var iDistance = percentToShow + Mathf.Clamp01(GrassProximityThreshold / (distance + 0.0000001f));

            return iDistance > Random.value;
        }

        public float GetSquaredDistanceToCamera(Matrix4x4 matrix, Camera camera) {
            Vector3 position = matrix.GetColumn(3);
            Vector3 cameraPosition = camera.transform.position;
            return (position - cameraPosition).sqrMagnitude;
        }

        private bool IsInFrustum(Vector3 position, Plane[] frustumPlanes) {
            foreach (Plane plane in frustumPlanes) {
                if (plane.GetDistanceToPoint(position) < 0) {
                    return false;
                }
            }

            return true;
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

                _grass.Clear();
                foreach (var c in ChunkRenderers) {
                    if (c is null || !c.isActiveAndEnabled) continue;
                    foreach (var (key, value) in c.Props) {
                        if (_mesh == null) _mesh = key;
                        _grass.AddRange(value);
                    }
                }

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
                cr.ReCalculateMesh(_level, new ChunkKey(LevelId, chX, chZ), ClientEngine.State.BlockPathById);
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
                    chunkRenderer.ReCalculateMesh(_level, new ChunkKey(LevelId, chX, chZ), ClientEngine.State.BlockPathById);
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