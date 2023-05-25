using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace VoxelsEngine {
    public class LevelGenerator : MonoBehaviour {
        private LevelData _level = null!;

        public Dictionary<string, ChunkRenderer> Chunks = new();
        public string SaveId = "test";
        public string LevelId = "0";

        [Required]
        public Material BlockMaterial = null!;

        [Required, SceneObjectsOnly]
        public Character Player = null!;

        private void Awake() {
            _level = new LevelData(SaveId, LevelId);
        }

        public void Update() {
            var playerPos = Player.transform.position;
            float cX = Mathf.Floor(playerPos.x);
            float cY = Mathf.Floor(playerPos.z);
            int chX = Mathf.FloorToInt(cX / 16);
            int chY = Mathf.FloorToInt(cY / 16);

            for (int x = -1; x <= 1; x++) {
                for (int y = -1; y <= 1; y++) {
                    var key = Chunk.GetKey(SaveId, LevelId, chX + x, chY + y);
                    if (!Chunks.ContainsKey(key)) {
                        GenerateNearChunk(chX + x, chY + y, playerPos, key).Forget();
                    }
                }
            }
        }

        private async UniTask GenerateNearChunk(int chX, int chY, Vector3 playerPos, string key) {
            Chunk? currentChunk = await _level.GetOrGenerateChunk(chX, chY, 1337 + chX + 100000 * chY);
            if (currentChunk != null) {
                var chunkGen = GenerateChunkRenderer(currentChunk, chX, chY);
                chunkGen.transform.SetParent(transform, true);
                await chunkGen.Redraw(_level);
                Chunks.Add(key, chunkGen);
            }
        }

        private ChunkRenderer GenerateChunkRenderer(Chunk currentChunk, int chX, int chY) {
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