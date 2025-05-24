using System;
using System.IO;
using Cysharp.Threading.Tasks;
using LoneStoneStudio.Tools;
using LoneStoneStudio.Tools.Components;
using MessagePack;
using Shared;
using Shared.Net;
using Sirenix.OdinInspector;
using TinkState;
using UnityEngine;
using UnityEngine.Serialization;
using VoxelsEngine.UI;
using Vector3 = Shared.Vector3;

namespace VoxelsEngine {
    // Add to ClientMain.cs
    public enum LoadingStage {
        NotStarted,
        Initializing,
        LocalCheckingSaveFile,
        LocalLoadingGameState,
        LocalCreatingGameState,
        ClientConnectingToServer,
        ClientAuthenticatingPlayer,
        LocalGeneratingChunks,
        LocalCreatingCharacter,
        UploadingToGPU,
        EnteringGame,
        Complete
    }

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
        public string ServerHost = "192.168.1.15";
        private ClientEngine? _engine;
        private PlayerCharacterAgent? _agent;
        private readonly PrefabListSynchronizer<CharacterAgent> _otherPlayersAgents = new();

        // Observable property to track loading progress
        public State<LoadingStage> CurrentLoadingStage = Observable.State(LoadingStage.NotStarted);
        public State<float> LoadingProgress = Observable.State(0f);

        public string SaveName = "gamesave.bin";
        private string LocalSavePath => Path.Join(Application.persistentDataPath, SaveName);

        [Button]
        public void OpenSaveFolder() {
            if (File.Exists(LocalSavePath)) {
                Application.OpenURL("file://" + Path.GetDirectoryName(LocalSavePath));
            } else {
                Logr.Log("No save file found", Tags.Standalone);
            }
        }

        private void Start() {
            _otherPlayersAgents.Container = gameObject;
            _otherPlayersAgents.Prefab = CharacterPrefab;
            if (ForceLocalPlay) StartLocalPlay().Forget();
            // SideEffectManager.For<CharacterJoinGameEvent>().Start(joinEvent);
            // StartRemotePlay().Forget();
        }

        // Add this method to receive data from JavaScript
        public void SetServerDetails(string host, int port) {
            ServerHost = host;
            ServerPort = port;
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
            DisplayLoading(LoadingStage.Initializing, 0.1f);

            _engine = gameObject.AddComponent<ClientEngine>();
            _engine.SideEffectManager.For<CharacterJoinGameEvent>().StartListening(HandlePlayerJoin);
            _engine.SideEffectManager.For<CharacterLeaveGameEvent>().StartListening(HandlePlayerLeave);

            DisplayLoading(LoadingStage.ClientConnectingToServer, 0.3f);

#if UNITY_WEBGL && !UNITY_EDITOR
            var url = Application.absoluteURL;
            string[] parameters = url.Split('?')[1].Split('&');
        
            foreach (string parameter in parameters) {
                string[] keyValue = parameter.Split('=');
                if (keyValue.Length == 2) {
                    string key = keyValue[0];
                    string value = keyValue[1];
                    if (key == "host") {
                        ServerHost = value;
                    }
                }
            }
#endif
            await _engine.InitRemote(ServerHost);
            DisplayLoading(LoadingStage.ClientAuthenticatingPlayer, 0.5f);

            Application.runInBackground = true;
            await Configurator.Instance.IsReady();
            _engine.State.UpdateBlockMapping(Configurator.Instance.BlockRegistry!);

            DisplayLoading(LoadingStage.Complete, 1.0f);
        }


        private async UniTask StartLocalPlay() {
            DisplayLoading(LoadingStage.Initializing, 0.05f);

            GameState? state = null;
            Configurator.Instance.IsReady().Forget();

            DisplayLoading(LoadingStage.LocalCheckingSaveFile, 0.1f);

            if (File.Exists(LocalSavePath)) {
                try {
                    DisplayLoading(LoadingStage.LocalLoadingGameState, 0.2f);

                    state = MessagePackSerializer.Deserialize<GameState>(await File.ReadAllBytesAsync(LocalSavePath));

                    await Configurator.Instance.IsReady();
                    state.UpdateBlockMapping(Configurator.Instance.BlockRegistry!);

                    Logr.Log("Loading existing game", Tags.Standalone);
                } catch (Exception e) {
                    Logr.LogException(e, $"Couldn't read from {LocalSavePath}");
                    _engine = null;
                    return;
                }
            }

            await Configurator.Instance.IsReady();

            try {
                // spawn in the middle
                var spawnPosition = new Vector3(
                    LevelMap.LevelChunkSize * Chunk.Size / 2f + 4,
                    8.5f,
                    LevelMap.LevelChunkSize * Chunk.Size / 2f + 4
                );
                var (spawnPositionChX, spawnPositionChZ) = LevelTools.GetChunkPosition(spawnPosition);

                if (state == null) {
                    DisplayLoading(LoadingStage.LocalCreatingGameState, 0.2f);

                    Logr.Log("Creating new game", Tags.Standalone);
                    state = new GameState(null, null, null);
                    state.UpdateBlockMapping(Configurator.Instance.BlockRegistry!);
                    var levelMap = new LevelMap("World", spawnPosition);
                    state.Levels.Add("World", levelMap);
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

                if (!state.Levels.ContainsKey("World")) {
                    throw new Exception("No world");
                }

                _engine = gameObject.AddComponent<ClientEngine>();

                // bootup local engine
                DisplayLoading(LoadingStage.LocalGeneratingChunks, 0.4f);

                _engine.State.UpdateValue(state);
                state = null;
                _engine.State.LevelGenerator.EnqueueUninitializedChunksAround("World", spawnPositionChX, spawnPositionChZ, 5, _engine.State.Levels);
                _engine.State.LevelGenerator.GenerateFromQueue(PriorityLevel.LoadingTime, _engine.State.Levels);

                DisplayLoading(LoadingStage.LocalCreatingCharacter, 0.7f);

                await AddPlayerCharacter(spawnPosition, 0);
                LocalState.Instance.CurrentPlayerId.Value = 0;
                LocalState.Instance.CurrentPlayerName = "Local";

                DisplayLoading(LoadingStage.EnteringGame, 0.9f);

                _engine.StartLocal();
                ConnectionModal.Instance.SmartActive(false);

                DisplayLoading(LoadingStage.Complete, 1f);
            } catch (Exception e) {
                Logr.LogException(e, $"Couldn't read from {LocalSavePath}");
                return;
            }
        }

        private void DisplayLoading(LoadingStage stage, float progress) {

            CurrentLoadingStage.Value = stage;
            LoadingProgress.Value = progress;
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