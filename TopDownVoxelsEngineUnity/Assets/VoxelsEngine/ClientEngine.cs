using System;
using System.Threading.Tasks;
using LoneStoneStudio.Tools;
using Shared;
using Shared.Net;
using Sirenix.OdinInspector;
using UnityEngine;
using VoxelsEngine.UI;
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
        public GameState State = new(null, null);

        public readonly SideEffectManager SideEffectManager = new();

        private readonly TickGameEvent _tick = new();
        private PriorityLevel _minLevel = PriorityLevel.All;
        private bool Started => LocalState.Instance.Session.Value == SessionStatus.Ready;
        private bool _receivedAtLeastOneChunkUpdate = false;

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
                foreach (var (_, c) in State.Characters) {
                    var (chx, chz) = LevelTools.GetChunkPosition(c.Position);
                    if (c.Level.Value != null && State.Levels.ContainsKey(c.Level.Value)) {
                        State.LevelGenerator.EnqueueUninitializedChunksAround(c.Level.Value, chx, chz, Configurator.Instance.RenderDistance, State.Levels);
                    }
                }

                State.LevelGenerator.GenerateFromQueue(_minLevel, State.Levels);
            } else {
                var c = State.Characters[LocalState.Instance.CurrentPlayerId];
                SocketClient.Send(
                    new CharacterMoveGameEvent(
                        0,
                        LocalState.Instance.CurrentPlayerId,
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

        public async Task InitRemote(int port) {
            try {
                SocketClient = new SocketClient();
                SocketClient.OnNetworkMessage += HandleNetMessage;
                SocketClient.OnConnexionLost += HandleConnexionLost;
                await SocketClient.Init("192.168.1.157", port);
                LocalState.Instance.Session.Value = SessionStatus.NeedAuthentication;
                await Task.Delay(500);
                SocketClient.Send(new HelloNetworkMessage(LocalState.Instance.CurrentPlayerName));
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
            if (SocketClient != null) {
                SocketClient.Close();
            }
        }
    }
}