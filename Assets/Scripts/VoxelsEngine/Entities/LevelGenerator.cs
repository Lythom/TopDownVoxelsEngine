using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace VoxelsEngine {
    public class LevelGenerator : MonoBehaviour {
        private LevelData _level = null!;

        public readonly List<ChunkKey> QueriedChunks = new();

        public string SaveId = "test";
        public string LevelId = "0";

        [Required]
        public Material BlockMaterial = null!;

        [Required, SceneObjectsOnly]
        public Character Player = null!;

        private void Awake() {
            _level = new LevelData(SaveId, LevelId);
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
                        GenerateNearChunk(chX + x, chZ + z).Forget();
                    }
                }
            }
        }

        private async UniTask GenerateNearChunk(int chX, int chY) {
            try {
                ChunkData? currentChunk = await _level.GetOrGenerateChunk(chX, chY);
                if (currentChunk != null) {
                    var chunkRenderer = GenerateChunkRenderer(currentChunk, chX, chY);
                    chunkRenderer.transform.SetParent(transform, true);
                    await UniTask.RunOnThreadPool(() => chunkRenderer.ReCalculateMesh(_level), true, gameObject.GetCancellationTokenOnDestroy());
                    // we return to MainThread here thanks to configureAwait: true, so we can update the mesh
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
            chunkGen.Chunk = currentChunk;
            return chunkGen;
        }
    }
}