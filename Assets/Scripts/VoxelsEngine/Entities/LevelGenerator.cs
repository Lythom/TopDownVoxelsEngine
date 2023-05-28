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

        public readonly List<ChunkKey> QueriedChunks = new();
        public readonly Queue<ChunkKey> ToBeRendererQueue = new();

        public string SaveId = "test";
        public string LevelId = "0";

        [Required]
        public Material BlockMaterial = null!;

        [Required, SceneObjectsOnly]
        public Character Player = null!;


        private void Awake() {
            _level = new LevelData(SaveId, LevelId);
            RenderChunksFromQueue(gameObject.GetCancellationTokenOnDestroy()).Forget();
        }

        private void OnDestroy() {
            _level.Dispose();
        }

        public void Update() {
            var playerPos = Player.transform.position;
            float cX = Mathf.Floor(playerPos.x);
            float cZ = Mathf.Floor(playerPos.z);
            int chX = Mathf.FloorToInt(cX / 16);
            int chZ = Mathf.FloorToInt(cZ / 16);

            for (int x = -1; x <= 1; x++) {
                for (int z = -1; z <= 1; z++) {
                    var key = ChunkData.GetKey(SaveId, LevelId, chX + x, chZ + z);
                    if (!QueriedChunks.Contains(key)) {
                        QueriedChunks.Add(key);
                        ToBeRendererQueue.Enqueue(key);
                    }
                }
            }
        }


        private async UniTask RenderChunksFromQueue(CancellationToken cancellationToken) {
            Debug.Log("Start job rendering chunks.");

            while (!cancellationToken.IsCancellationRequested) {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                // dequeue until all is generated
                while (ToBeRendererQueue.TryDequeue(out ChunkKey k)) {
                    try {
                        await GenerateNearChunk(k.ChX, k.ChZ);
                        // max 1 per frame
                        await UniTask.NextFrame(cancellationToken);
                    } catch (Exception e) {
                        Debug.LogException(e);
                    }
                }
            }

            Debug.Log("Stop job rendering chunks. Cancellation was requested.");
        }

        private async UniTask GenerateNearChunk(int chX, int chY) {
            Debug.Log("GenerateNearChunk");
            try {
                ChunkData currentChunk = await _level.GetOrGenerateChunk(chX, chY);
                if (currentChunk.IsGenerated) {
                    var chunkRenderer = GenerateChunkRenderer(currentChunk, chX, chY);
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