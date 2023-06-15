using System;
using System.IO;
using Cysharp.Threading.Tasks;
using MessagePack;
using Shared;
using Shared.Net;
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

        public string ServerURL = "ws://localhost:8080";
        private ClientEngine? _engine;

        private static string LocalSavePath => Path.Join(Application.persistentDataPath, "gamesave.bin");

        private void Awake() {
            //StartLocalPlay().Forget();
            StartRemotePlay().Forget();
        }

        private void OnDestroy() {
            if (_engine != null) _engine.SocketManager.Close();
        }

        private async UniTask StartRemotePlay() {
            _engine = gameObject.AddComponent<ClientEngine>();
            await _engine.InitRemote(ServerURL);
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
                    state.Levels.Add("World", new LevelMap("World", spawnPosition));
                    state.Characters.Add(0,
                        new Character(
                            "Local",
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

                _engine = gameObject.AddComponent<ClientEngine>();

                // bootup local engine
                _engine.State.UpdateValue(state);
                state = null;
                _engine.State.LevelGenerator.EnqueueUninitializedChunksAround("World", spawnPositionChX, spawnPositionChZ, 5, _engine.State.Levels);
                _engine.State.LevelGenerator.GenerateFromQueue(PriorityLevel.LoadingTime, _engine.State.Levels);

                var agent = Instantiate(CharacterPrefab, _engine.transform, true);
                agent.CharacterId = 0;
                agent.CameraTransform = Tracker.transform;
                Tracker.Target = agent.gameObject;

                // give a few tracker update ticks to place the camera correctly ahaead
                Tracker.transform.position = spawnPosition;
                for (int i = 0; i < 60; i++) {
                    Tracker.LateUpdate();
                }

                await UniTask.Delay(2000);

                _engine.Start();
            } catch (Exception e) {
                Logr.LogException(e, $"Couldn't read from {LocalSavePath}");
                return;
            }
        }
    }
}