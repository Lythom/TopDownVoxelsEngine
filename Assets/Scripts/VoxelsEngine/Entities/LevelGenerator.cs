using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace VoxelsEngine {
    public class LevelGenerator : MonoBehaviour {
        private LevelData _level = null!;

        public readonly HashSet<int> RendererChunks = new();
        public readonly Queue<int> ToBeRendererQueue = new();

        public string SaveId = "test";
        public string LevelId = "0";

        [Required]
        public Material BlockMaterial = null!;

        [Required, SceneObjectsOnly]
        public Character Player = null!;

        private CancellationToken _cancellationTokenOnDestroy;


        private void Awake() {
            _level = new LevelData(SaveId, LevelId);
            _cancellationTokenOnDestroy = gameObject.GetCancellationTokenOnDestroy();
            RenderChunksFromQueue(_cancellationTokenOnDestroy).Forget();
        }

        private void OnDestroy() {
            _level.Dispose();
        }

        public void Update() {
            var playerPos = Player.transform.position;
            int cX = Mathf.RoundToInt(playerPos.x + 8);
            int cZ = Mathf.RoundToInt(playerPos.z + 8);
            int chX = cX / 16;
            int chZ = cZ / 16;

            var range = 2;
            for (int x = -range; x <= range; x++) {
                for (int z = -range; z <= range; z++) {
                    var key = ChunkData.GetFlatIndex(chX + x, chZ + z);
                    if (!RendererChunks.Contains(key)) {
                        RendererChunks.Add(key);
                        ToBeRendererQueue.Enqueue(key);
                    }
                }
            }
        }

        private void OnDrawGizmos() {
            var playerPos = Player.transform.position;
            int cX = Mathf.RoundToInt(playerPos.x + 8);
            int cZ = Mathf.RoundToInt(playerPos.z + 8);
            int chX = cX / 16;
            int chZ = cZ / 16;
            Gizmos.DrawWireCube(new Vector3(chX * 16, 0, chZ * 16), new Vector3(16, 16, 16));
        }

        private async UniTask RenderChunksFromQueue(CancellationToken cancellationToken) {
            Debug.Log("Start job rendering chunks.");

            while (!cancellationToken.IsCancellationRequested) {
                var renderedThisFrame = 0;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                // dequeue until all is generated
                while (ToBeRendererQueue.TryDequeue(out int chunkFlatIndex)) {
                    try {
                        var (chX, chZ) = ChunkData.GetCoordsFromIndex(chunkFlatIndex);
                        await RenderChunk(chX, chZ);
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

        private async UniTask RenderChunk(int chX, int chZ) {
            try {
                ChunkData currentChunk = await _level.GetOrGenerateChunk(chX, chZ);
                // preload outbounds chunk content
                for (int x = -1; x <= 1; x++) {
                    for (int z = -1; z <= 1; z++) {
                        await _level.GetOrGenerateChunk(chX + x, chZ + z);
                    }
                }

                if (currentChunk.IsGenerated && !_cancellationTokenOnDestroy.IsCancellationRequested) {
                    var chunkRenderer = GenerateChunkRenderer(currentChunk, chX, chZ);
                    chunkRenderer.transform.SetParent(transform, true);
                    chunkRenderer.ReCalculateMesh(_level);
                    chunkRenderer.UpdateMesh();
                    chunkRenderer.transform.localScale = Vector3.zero;
                    chunkRenderer.transform.DOScale(1, 0.3f).SetEase(Ease.OutBack);
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        private ChunkRenderer GenerateChunkRenderer(ChunkData currentChunk, int chX, int chY) {
            var go = new GameObject("Chunk Renderer " + currentChunk.ChX + "," + currentChunk.ChZ);
            var f = go.AddComponent<MeshFilter>();
            f.mesh = new Mesh();
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = BlockMaterial;
            var chunkGen = go.AddComponent<ChunkRenderer>();
            chunkGen.transform.localPosition = new Vector3(chX * 16, 0, chY * 16);
            chunkGen.Level = _level;
            chunkGen.ChunkKey = currentChunk.GetKey(_level.SaveId, _level.LevelId);
            return chunkGen;
        }
    }
}