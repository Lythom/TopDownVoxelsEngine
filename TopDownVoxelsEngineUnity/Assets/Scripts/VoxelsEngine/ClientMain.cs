using System;
using System.IO;
using Cysharp.Threading.Tasks;
using MessagePack;
using Shared;
using Shared.Net;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Vector3 = Shared.Vector3;

namespace VoxelsEngine {
    // Drive workflows and game logique at high level
    public class ClientMain : MonoBehaviour {
        [Required, SceneObjectsOnly]
        public CameraTracker Tracker = null!;

        [FormerlySerializedAs("CharacterPrefab")]
        [Required, AssetsOnly]
        public PlayerCharacterAgent PlayerCharacterPrefab = null!;

        public CharacterAgent CharacterPrefab = null!;

        public int ServerPort = 9999;
        private ClientEngine? _engine;
        private PlayerCharacterAgent? _agent;

        private static string LocalSavePath => Path.Join(Application.persistentDataPath, "gamesave.bin");

        private void Awake() {
            //StartLocalPlay().Forget();
            // SideEffectManager.For<CharacterJoinGameEvent>().Start(joinEvent);
            // StartRemotePlay().Forget();
        }

        public void HandlePlayerJoin(CharacterJoinGameEvent joinEvent) {
            if (joinEvent.Character.Name == LocalState.Instance.CurrentPlayerName) {
                AddPlayerCharacter(joinEvent.Character.Position, joinEvent.CharacterShortId).Forget();
            } else {
                AddOtherCharacter(joinEvent.Character.Position, joinEvent.CharacterShortId);
            }
        }

        private void OnDestroy() {
            if (_engine != null) _engine.SocketClient.Close();
        }

        public async UniTask StartRemotePlay() {
            _engine = gameObject.AddComponent<ClientEngine>();
            _engine.SideEffectManager.For<CharacterJoinGameEvent>().StartListening(HandlePlayerJoin);
            await _engine.InitRemote(ServerPort);
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

                await AddPlayerCharacter(spawnPosition, 0);

                _engine.StartLocal();
            } catch (Exception e) {
                Logr.LogException(e, $"Couldn't read from {LocalSavePath}");
                return;
            }
        }

        public async UniTask AddPlayerCharacter(Vector3 spawnPosition, ushort shortId) {
            _agent = Instantiate(PlayerCharacterPrefab, _engine!.transform, true);
            _agent.CharacterId = shortId;
            _agent.CameraTransform = Tracker.transform;
            _agent.transform.position = spawnPosition;
            Tracker.Target = _agent.gameObject;

            // give a few tracker update ticks to place the camera correctly ahaead
            Tracker.transform.position = spawnPosition;
            for (int i = 0; i < 60; i++) {
                Tracker.LateUpdate();
            }

            await UniTask.Delay(1000);
        }

        public void AddOtherCharacter(Vector3 spawnPosition, ushort shortId) {
            var agent = Instantiate(CharacterPrefab, _engine!.transform, true);
            agent.CharacterId = shortId;
            agent.transform.position = spawnPosition;
        }
    }
}