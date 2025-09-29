using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using LoneStoneStudio.Tools;
using MessagePack;
using Shared;
using Shared.Net;
using Sirenix.OdinInspector;
using UnityEngine;
using VoxelsEngine.UI;
using ZLinq;
using Vector3 = UnityEngine.Vector3;

namespace VoxelsEngine {
    /// <summary>
    ///  Le rôle de client engine est de
    /// - servir de serveur au client local sans qu'il ait besoin de passer par un serveur distant.
    /// - Simuler les actions d'un serveur localement (synchronisation d'états)
    /// </summary>
    public class ClientEngine : MonoBehaviour {
        public ISocketClient SocketClient = new FakeEchoingSocketClient();
        public bool IsLocalEngine() => SocketClient is FakeEchoingSocketClient;

        [ShowInInspector]
        public GameState State = new();

        public readonly SideEffectManager SideEffectManager = new();
        public readonly Selectors Selectors;

        private readonly TickGameEvent _tick = new();
        private PriorityLevel _minLevel = PriorityLevel.All;
        private bool Started => LocalState.Instance.Session.Value == SessionStatus.Ready;
        private bool _receivedAtLeastOneChunkUpdate = false;

        private readonly Dictionary<Guid, BlueprintV0> _localBlueprints = new();
        private readonly List<BlueprintMetadataV0> _localBlueprintMetadata = new();
        private readonly string LocalBlueprintPath = Path.Combine(Application.persistentDataPath, "blueprints");

        public ClientEngine() {
            Selectors = new(State);
        }

        private void HandleNetMessage(INetworkMessage msg) {
            if (msg is not CharacterMoveGameEvent) Logr.Log("Received " + msg, Tags.Client);
            switch (msg) {
                case CharacterJoinGameEvent joinEvent:
                    if (joinEvent.Character.Name == LocalState.Instance.CurrentPlayerName) {
                        if (LocalState.Instance.Session.Value != SessionStatus.NeedAuthentication) {
                            throw new ApplicationException($"Error in the flow: the player is either already authenticated or not ready no be. Current status = {LocalState.Instance.Session.Value}. Expected: {SessionStatus.NeedAuthentication}");
                        }

                        LocalState.Instance.CurrentPlayerId.Value = joinEvent.CharacterShortId;
                        SideEffectManager.For<PriorityLevel>().StopListening(UpdatePriorityLevel);
                        SideEffectManager.For<PriorityLevel>().StartListening(UpdatePriorityLevel);
                        LocalState.Instance.Session.Value = _receivedAtLeastOneChunkUpdate ? SessionStatus.Ready : SessionStatus.GettingReady;
                    }

                    HandleEvent(joinEvent);
                    SideEffectManager.For<CharacterJoinGameEvent>().Trigger(joinEvent);
                    break;
                case CharacterMoveGameEvent moveEvent:
                    if (LocalState.Instance.Session.Value != SessionStatus.Ready) break;
                    if (moveEvent.CharacterShortId == LocalState.Instance.CurrentPlayerId.Value) {
                        // apply event to fix position only if the mismatch is important (cheating ?).
                        if (Vector3.Distance(moveEvent.Position, transform.position) > 1) {
                            HandleEvent(moveEvent);
                        }
                    } else if (State.Characters.ContainsKey(moveEvent.CharacterShortId)) {
                        HandleEvent(moveEvent);
                    }

                    break;
                case ChunkUpdateGameEvent cuge:
                    if (!_receivedAtLeastOneChunkUpdate) {
                        LocalState.Instance.Session.Value = LocalState.Instance.CurrentPlayerId.Value != ushort.MaxValue ? SessionStatus.Ready : SessionStatus.GettingReady;
                        _receivedAtLeastOneChunkUpdate = true;
                    }

                    HandleEvent(cuge);
                    break;

                // Handle blueprint messages locally when in local mode
                case SaveBlueprintCommand saveBlueprintCmd when IsLocalEngine():
                    HandleLocalSaveBlueprint(saveBlueprintCmd);
                    break;
                case LoadBlueprintListQuery loadListQuery when IsLocalEngine():
                    HandleLocalLoadBlueprintList(loadListQuery);
                    break;
                case LoadBlueprintQuery loadQuery when IsLocalEngine():
                    HandleLocalLoadBlueprint(loadQuery);
                    break;
                case PlaceBlueprintCommand placeCmd when IsLocalEngine():
                    HandleLocalPlaceBlueprint(placeCmd);
                    break;

                case IGameEvent gameEvent:
                    HandleEvent(gameEvent);
                    break;
                case ErrorNetworkMessage err:
                    Debug.LogError("[Server Error] " + err.Message);
                    SocketClient.Close();
                    break;
            }
        }

        public void StartLocal() {
            SocketClient.OnNetworkMessage -= HandleNetMessage;
            SocketClient.OnNetworkMessage += HandleNetMessage;
            SideEffectManager.For<PriorityLevel>().StopListening(UpdatePriorityLevel);
            SideEffectManager.For<PriorityLevel>().StartListening(UpdatePriorityLevel);
            LoadLocalBlueprintsFromFile();
            LocalState.Instance.Session.Value = SessionStatus.Ready;
        }

        public void Stop() {
            LocalState.Instance.Session.Value = SessionStatus.Disconnected;
            SocketClient.OnNetworkMessage -= HandleNetMessage;
            SideEffectManager.For<PriorityLevel>().StopListening(UpdatePriorityLevel);
        }

        private void UpdatePriorityLevel(PriorityLevel l) {
            _minLevel = l;
        }

        private void FixedUpdate() {
            if (!Started) return;
            _tick.Id++;
            _tick.MinPriority = _minLevel;
            _tick.Apply(State, SideEffectManager);

            // When this is run by the client for himself, ne need to ask server
            if (IsLocalEngine()) {
                // TODO:to be part of TickGameEvent ?
                foreach (var (_, c) in State.Characters) {
                    LevelTools.GetChunkPosition(c.Position, out var chX, out var chZ);
                    ;
                    if (c.Level.Value != null && State.Levels.ContainsKey(c.Level.Value)) {
                        State.LevelGenerator.EnqueueUninitializedChunksAround(c.Level.Value, chX, chZ, Configurator.Instance.RenderDistance, State.Levels);
                    }
                }

                State.LevelGenerator.GenerateFromQueue(_minLevel, State.Levels);
            } else {
                var c = State.Characters[LocalState.Instance.CurrentPlayerId.Value];
                SocketClient.Send(
                    new CharacterMoveGameEvent(
                        0,
                        LocalState.Instance.CurrentPlayerId.Value,
                        c.Position,
                        c.Velocity,
                        c.Angle,
                        c.IsInAir
                    )
                );
            }
        }

        public void HandleEvent(IGameEvent evt) {
            evt.AssertApplicationConditions(State);
            evt.Apply(State, SideEffectManager);
        }

        public async Task InitRemote(string host) {
            try {
                Logr.Log("Connecting to " + host, Tags.Client);
                SocketClient = new NativeSocketClient();
                SocketClient.OnNetworkMessage += HandleNetMessage;
                SocketClient.OnConnexionLost += HandleConnexionLost;
                await SocketClient.Init(host);
                Logr.Log("Connected. Delaying before loading…", Tags.Client);
                LocalState.Instance.Session.Value = SessionStatus.NeedAuthentication;
                await UniTask.Delay(500);
                Logr.Log("Sending RegisterPlayerCommand", Tags.Client);
                SocketClient.Send(new RegisterPlayerCommand(LocalState.Instance.CurrentPlayerName));
            } catch (Exception e) {
                Logr.LogException(e);
                HandleConnexionLost();
                throw;
            }
        }

        private void HandleConnexionLost() {
            LocalState.Instance.Session.Value = SessionStatus.Disconnected;
            if (this == null || !Application.isPlaying) return;
            transform.DestroyChildren();
            Destroy(this);
            if (ConnectionModal.Instance != null) ConnectionModal.Instance.SmartActive(true);
        }

        private void OnDestroy() {
            SocketClient.Close();
        }

        #region Blueprints

        private void HandleLocalSaveBlueprint(SaveBlueprintCommand cmd) {
            try {
                // Get the character and level
                if (!State.Characters.TryGetValue(cmd.CharacterShortId, out var character) ||
                    character.Level.Value == null ||
                    !State.Levels.TryGetValue(character.Level.Value, out var levelMap)) {
                    Logr.LogError("Cannot save blueprint: invalid character or level");
                    return;
                }

                // Create blueprint from world data
                var (blueprint, error) = LevelTools.CreateBlueprint(
                    LocalState.Instance.CurrentPlayerName ?? "Player",
                    cmd.Name,
                    new(cmd.AnchorX, cmd.AnchorY, cmd.AnchorZ),
                    new(cmd.SizeX, cmd.SizeY, cmd.SizeZ),
                    levelMap,
                    State.BlockMapping);

                if (blueprint == null || error != null) {
                    var message = $"Cannot save blueprint: {error ?? "Unkown error"}";
                    Logr.LogError(message);
                    SocketClient.Send(new AckResponse(cmd.Id, message));
                    return;
                }

                // Store locally
                _localBlueprints[blueprint.Id] = blueprint;
                _localBlueprintMetadata.Add(new BlueprintMetadataV0(
                    blueprint.Id,
                    cmd.Name,
                    blueprint.CreatorId,
                    blueprint.CreationDate,
                    blueprint.LastModifiedDate,
                    blueprint.Size,
                    blueprint.FloorHeight,
                    blueprint.PossibleSymmetries
                ));

                // Save to disk
                SaveLocalBlueprintsToFile();

                // Send ACK (echo back the command as confirmation)
                SocketClient.Send(new AckResponse(cmd.Id, null));
            } catch (Exception e) {
                Logr.LogException(e);
            }
        }

        private void HandleLocalLoadBlueprintList(LoadBlueprintListQuery query) {
            try {
                var startIndex = query.Page * query.PageSize;
                var blueprints = _localBlueprintMetadata
                    .AsValueEnumerable()
                    .OrderByDescending(b => b.LastModifiedDate)
                    .Skip(startIndex)
                    .Take(query.PageSize)
                    .ToArray();

                var response = new LoadBlueprintListResponse(
                    query.Id,
                    query.CharacterShortId,
                    blueprints,
                    _localBlueprintMetadata.Count
                );

                SocketClient.Send(response);
            } catch (Exception e) {
                Logr.LogException(e);
            }
        }

        private void HandleLocalLoadBlueprint(LoadBlueprintQuery query) {
            try {
                if (_localBlueprints.TryGetValue(query.BlueprintId, out var blueprint)) {
                    var response = new LoadBlueprintResponse(query.Id, query.CharacterShortId, blueprint);
                    SocketClient.Send(response);
                } else {
                    Logr.LogError($"Blueprint {query.BlueprintId} not found locally");
                }
            } catch (Exception e) {
                Logr.LogException(e);
            }
        }

        private void HandleLocalPlaceBlueprint(PlaceBlueprintCommand cmd) {
            try {
                if (!_localBlueprints.TryGetValue(cmd.BlueprintId, out var blueprint)) {
                    Logr.LogError($"Blueprint {cmd.BlueprintId} not found locally");
                    return;
                }

                if (!State.Characters.TryGetValue(cmd.CharacterShortId, out var character) ||
                    character.Level.Value == null ||
                    !State.Levels.TryGetValue(character.Level.Value, out var levelMap)) {
                    Logr.LogError("Cannot place blueprint: invalid character or level");
                    return;
                }

                // Apply blueprint to world
                var modifiedChunks = new HashSet<ChunkKey>();
                levelMap.PlaceBlueprint(cmd.X, cmd.Y, cmd.Z, cmd.Rotation, cmd.FlipOperations, blueprint, modifiedChunks);
                foreach (var c in modifiedChunks) {
                    SocketClient.Send(new ChunkUpdateGameEvent(0, levelMap.LevelId, levelMap.Chunks[c.ChX, c.ChZ], c.ChX, c.ChZ));
                }
            } catch (Exception e) {
                Logr.LogException(e);
            }
        }

        private void SaveLocalBlueprintsToFile() {
            try {
                if (!Directory.Exists(LocalBlueprintPath)) {
                    Directory.CreateDirectory(LocalBlueprintPath);
                }

                var data = new Dictionary<string, object> {
                    ["blueprints"] = _localBlueprints.Values.AsValueEnumerable().ToArray(),
                    ["metadata"] = _localBlueprintMetadata.ToArray()
                };

                var bytes = MessagePackSerializer.Serialize(data);
                File.WriteAllBytes(Path.Combine(LocalBlueprintPath, "local_blueprints.msgpack"), bytes);
            } catch (Exception e) {
                Logr.LogException(e);
            }
        }

        private void LoadLocalBlueprintsFromFile() {
            try {
                var filePath = Path.Combine(LocalBlueprintPath, "local_blueprints.msgpack");
                if (!File.Exists(filePath)) return;

                var bytes = File.ReadAllBytes(filePath);
                var data = MessagePackSerializer.Deserialize<Dictionary<string, object>>(bytes);

                if (data.TryGetValue("blueprints", out var blueprintsObj) && blueprintsObj is BlueprintV0[] blueprints) {
                    foreach (var blueprint in blueprints) {
                        _localBlueprints[blueprint.Id] = blueprint;
                    }
                }

                if (data.TryGetValue("metadata", out var metadataObj) && metadataObj is BlueprintMetadataV0[] metadata) {
                    _localBlueprintMetadata.AddRange(metadata);
                }
            } catch (Exception e) {
                Logr.LogException(e);
            }
        }

        #endregion
    }
}