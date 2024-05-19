using System;
using System.IO;
using Cysharp.Threading.Tasks;
using LoneStoneStudio.Tools;
using LoneStoneStudio.Tools.Components;
using MessagePack;
using Shared;
using Shared.Net;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using VoxelsEngine.UI;
using Vector3 = Shared.Vector3;

namespace VoxelsEngine {
    // Drive workflows and game logique at high level
    public class ClientMain : MonoBehaviour {
        public bool ForceLocalPlay = false;

        [Required, SceneObjectsOnly]
        public CameraTracker Tracker = null!;

        [FormerlySerializedAs("CharacterPrefab")]
        [Required, AssetsOnly]
        public PlayerCharacterAgent PlayerCharacterPrefab = null!;

        public CharacterAgent CharacterPrefab = null!;

        public int ServerPort = 9999;
        private ClientEngine? _engine;
        private PlayerCharacterAgent? _agent;
        private readonly PrefabListSynchronizer<CharacterAgent> _otherPlayersAgents = new();

        private static string LocalSavePath => Path.Join(Application.persistentDataPath, "gamesave.bin");

        private void Awake() {
            _otherPlayersAgents.Container = gameObject;
            _otherPlayersAgents.Prefab = CharacterPrefab;
            if (ForceLocalPlay) StartLocalPlay().Forget();
            // SideEffectManager.For<CharacterJoinGameEvent>().Start(joinEvent);
            // StartRemotePlay().Forget();
        }

        public void HandlePlayerJoin(CharacterJoinGameEvent joinEvent) {
            if (joinEvent.Character.Name == LocalState.Instance.CurrentPlayerName) {
                AddPlayerCharacter(joinEvent.Character.Position, joinEvent.CharacterShortId).Forget();
            } else {
                UpdateAgents();
            }
        }

        private void HandlePlayerLeave(CharacterLeaveGameEvent evt) {
            if (evt.CharacterShortId == LocalState.Instance.CurrentPlayerId) {
                // disconnect current player
                if (_engine != null) {
                    _engine.Stop();
                }
            } else {
                UpdateAgents();
            }
        }

        private void UpdateAgents() {
            if (_engine != null) {
                _otherPlayersAgents.DisplayInstances(declare => {
                    foreach (var (key, value) in _engine.State.Characters) {
                        if (key != LocalState.Instance.CurrentPlayerId) {
                            var agent = declare();
                            agent.CharacterId.Value = key;
                            agent.transform.position = value.Position;
                        }
                    }
                });
            }
        }

        private void OnDestroy() {
            if (_engine != null) {
                if (ForceLocalPlay) {
                    var state = MessagePackSerializer.Serialize(_engine.State);
                    File.WriteAllBytes(LocalSavePath, state);
                    // File.WriteAllText(LocalSavePath + ".json", MessagePackSerializer.ConvertToJson(state));
                    Logr.Log("writing game save at: " + LocalSavePath);
                }

                _engine.Stop();
                _engine.SideEffectManager.For<CharacterJoinGameEvent>().StopListening(HandlePlayerJoin);
                _engine.SideEffectManager.For<CharacterLeaveGameEvent>().StopListening(HandlePlayerLeave);
            }
        }

        public async UniTask StartRemotePlay() {
            _engine = gameObject.AddComponent<ClientEngine>();
            _engine.SideEffectManager.For<CharacterJoinGameEvent>().StartListening(HandlePlayerJoin);
            _engine.SideEffectManager.For<CharacterLeaveGameEvent>().StartListening(HandlePlayerLeave);
            await _engine.InitRemote(ServerPort);
            Application.runInBackground = true;
        }


        private async UniTask StartLocalPlay() {
            GameState? state = null;
            if (File.Exists(LocalSavePath)) {
                try {
                    state = MessagePackSerializer.Deserialize<GameState>(await File.ReadAllBytesAsync(LocalSavePath));
                    Logr.Log("Loading existing game", Tags.Standalone);
                } catch (Exception e) {
                    Logr.LogException(e, $"Couldn't read from {LocalSavePath}");
                    _engine = null;
                    return;
                }
            }

            try {
                // spawn in the middle
                var spawnPosition = new Vector3(
                    LevelMap.LevelChunkSize * Chunk.Size / 2f + 4,
                    8.5f,
                    LevelMap.LevelChunkSize * Chunk.Size / 2f + 4
                );
                var (spawnPositionChX, spawnPositionChZ) = LevelTools.GetChunkPosition(spawnPosition);

                if (state == null) {
                    Logr.Log("Creating new game", Tags.Standalone);
                    state = new GameState(null, null);
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
                LocalState.Instance.CurrentPlayerId.Value = 0;
                LocalState.Instance.CurrentPlayerName = "Local";

                _engine.StartLocal();
                ConnectionModal.Instance.SmartActive(false);
            } catch (Exception e) {
                Logr.LogException(e, $"Couldn't read from {LocalSavePath}");
                return;
            }
        }

        public async UniTask AddPlayerCharacter(Vector3 spawnPosition, ushort shortId) {
            _agent = Instantiate(PlayerCharacterPrefab, _engine!.transform, true);
            _agent.Init(shortId, Tracker.GetComponent<Camera>(), spawnPosition);
            Tracker.Target = _agent.gameObject;

            // give a few tracker update ticks to place the camera correctly ahaead
            Tracker.transform.position = spawnPosition;
            for (int i = 0; i < 60; i++) {
                Tracker.LateUpdate();
            }

            await UniTask.Delay(1000);
        }
    }
}