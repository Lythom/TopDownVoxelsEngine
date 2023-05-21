using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace VoxelsEngine {
    public class LevelGenerator : MonoBehaviour {
        public readonly LevelData Level;
        public readonly Dictionary<string, ChunkGenerator> Chunks;

        [Required, SceneObjectsOnly]
        public Character Player;

        public LevelGenerator() {
            Level = new LevelData();
            Chunks = new Dictionary<string, ChunkGenerator>();
        }

        public async void Update() {
            var playerPos = Player.transform.position;
            float cX = Mathf.Floor(playerPos.x);
            float cY = Mathf.Floor(playerPos.z);
            int chX = Mathf.FloorToInt(cX / 16);
            int chY = Mathf.FloorToInt(cY / 16);
            string currentChunkKey = Chunk.GetKey("test", "0", chX, chY);

            Chunk? currentChunk = await Level.GetOrGenerateChunk("test", "0", chX, chY, Mathf.RoundToInt(playerPos.x), Mathf.RoundToInt(playerPos.y), 1337);
            if (!Chunks.ContainsKey(currentChunkKey) && currentChunk != null) {
                var go = new GameObject("Chunk Generator " + currentChunk.ChX + "," + currentChunk.ChY);
                var chunkGen = go.AddComponent<ChunkGenerator>();
                chunkGen.transform.localPosition = new Vector3(chX * 16, 0, chY * 16);
                chunkGen.Chunk = currentChunk;
                chunkGen.Redraw();
                // Missing equivalent of Haxe's `add(cv)` here
                Chunks.Add(currentChunkKey, chunkGen);
            }
        }

        public void Redraw(Orientation o) {
            foreach (var (key, chunkGenerator) in Chunks) {
                chunkGenerator.Redraw();
            }
        }
    }
}