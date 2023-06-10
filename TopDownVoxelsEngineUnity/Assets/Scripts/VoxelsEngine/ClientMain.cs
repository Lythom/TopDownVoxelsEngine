using System;
using System.IO;
using Cysharp.Threading.Tasks;
using MessagePack;
using Shared;
using Sirenix.OdinInspector;
using UnityEngine;
using Vector3 = Shared.Vector3;

namespace VoxelsEngine {
    // Drive workflows and game logique at high level
    public class ClientMain : MonoBehaviour {
        [Required, SceneObjectsOnly]
        public CameraTracker Tracker = null!;

        [Required, AssetsOnly]
        public CharacterAgent CharacterPrefab = null!;

        private static string LocalSavePath => Path.Join(Application.persistentDataPath, "gamesave.bin");

        private void Awake() {
            StartLocalPlay().Forget();
        }

        private async UniTask StartLocalPlay() {
            GameState? state = null;
            if (File.Exists(LocalSavePath)) {
                try {
                    state = MessagePackSerializer.Deserialize<GameState>(await File.ReadAllBytesAsync(LocalSavePath));
                    Logr.Log("Loading existing game");
                } catch (Exception e) {
                    Logr.LogException(e, $"Couldn't read from {LocalSavePath}");
                    return;
                }
            }

            try {
                // spawn in the middle
                var spawnPosition = new Vector3(
                    LevelMap.LevelChunkSize * Chunk.Size / 2f + 4,
                    4.5f,
                    LevelMap.LevelChunkSize * Chunk.Size / 2f + 4
                );
                var (spawnPositionChX, spawnPositionChZ) = LevelTools.GetChunkPosition(spawnPosition);

                if (state == null) {
                    Logr.Log("Creating new game");
                    state = new GameState(null, null, null);
                    state.Levels.Add("World", new LevelMap("World"));
                    state.Characters.Add(0,
                        new Character(
                            new Vector3(LevelMap.LevelChunkSize * Chunk.Size / 2f + 4, 10f, LevelMap.LevelChunkSize * Chunk.Size / 2f + 4),
                            Vector3.zero,
                            0,
                            new("World"),
                            null,
                            null,
                            null,
                            null,
                            null,
                            null,
                            null,
                            null,
                            null
                        )
                    );
                }

                var engine = gameObject.AddComponent<ClientEngine>();
                engine.State.UpdateValue(state);
                state = null;
                engine.State.LevelGenerator.EnqueueChunksAround("World", spawnPositionChX, spawnPositionChZ, 5, engine.State.Levels);
                engine.State.LevelGenerator.GenerateFromQueue(PriorityLevel.LoadingTime, engine.State.Levels);

                var agent = Instantiate(CharacterPrefab, engine.transform, true);
                agent.CharacterId = 0;
                agent.CameraTransform = Tracker.transform;
                Tracker.Target = agent.gameObject;

                // give a few tracker update ticks to place the camera correctly ahaead
                Tracker.transform.position = spawnPosition;
                for (int i = 0; i < 60; i++) {
                    Tracker.LateUpdate();
                }

                await UniTask.Delay(2000);

                engine.Start();
            } catch (Exception e) {
                Logr.LogException(e, $"Couldn't read from {LocalSavePath}");
                return;
            }
        }
    }
}